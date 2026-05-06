package generator

import (
	"context"
	"encoding/json"
	"fmt"
	"sync"
	"time"

	"case-generator/internal/model"
)

func (s *Service) AnalyzeScenarios(ctx context.Context, req model.AnalyzeRequest) (model.AnalyzeResponse, error) {
	var lastErr error
	feedback := ""

	for attempt := 1; attempt <= 3; attempt++ {
		userPrompt := fmt.Sprintf("Nghiệp vụ / Lệnh của sếp: %s\n", req.Prompt)
		if len(req.CurrentScenarios) > 0 {
			scenJSON, _ := json.MarshalIndent(req.CurrentScenarios, "", "  ")
			userPrompt += fmt.Sprintf("\nDanh sách kịch bản hiện tại:\n%s\n", string(scenJSON))
		}
		if req.FocusScenario != nil {
			focJSON, _ := json.MarshalIndent(req.FocusScenario, "", "  ")
			userPrompt += fmt.Sprintf("\nĐang Focus vào kịch bản:\n%s\n", string(focJSON))
		}
		if len(req.ChatHistory) > 0 {
			userPrompt += "\nLịch sử chat gần đây:\n"
			for _, msg := range req.ChatHistory {
				userPrompt += fmt.Sprintf("[%s]: %s\n", msg.Role, msg.Content)
			}
		}
		if feedback != "" {
			userPrompt += fmt.Sprintf("\nFeedback validator lần trước:\n%s\nHãy sửa lại và chỉ trả về JSON object gồm analysis và scenarios.\n", feedback)
		}

		text, err := s.llm.Complete(ctx, scenarioPlannerPrompt, userPrompt, 0.6)
		if err != nil {
			return model.AnalyzeResponse{}, err
		}

		var plan model.ScenarioPlan
		if err := json.Unmarshal([]byte(extractJSONObject(text)), &plan); err != nil {
			lastErr = err
			feedback = "JSON object lỗi cú pháp: " + err.Error()
			continue
		}

		normalizeScenarioBriefs(plan.Analysis, plan.Scenarios)
		issues := validator.ValidateScenarioPlan(plan)
		if len(issues) == 0 {
			validator.SortScenarioBriefs(plan.Scenarios)
			return model.AnalyzeResponse{
				Message: "Đã cập nhật danh sách kịch bản.",
				Scenarios: plan.Scenarios,
			}, nil
		}
		feedback = joinIssues(issues)
		lastErr = fmt.Errorf(feedback)

		select {
		case <-ctx.Done():
			return model.AnalyzeResponse{}, ctx.Err()
		case <-time.After(500 * time.Millisecond):
		}
	}

	return model.AnalyzeResponse{}, fmt.Errorf("failed after 3 attempts: %w", lastErr)
}

func (s *Service) GenerateSkeletons(ctx context.Context, req model.GenerateSkeletonsRequest) (model.GenerateSkeletonsResponse, error) {
	resp := model.GenerateSkeletonsResponse{
		Skeletons: make(map[string][]model.SkeletonNode),
	}
	var mu sync.Mutex
	var wg sync.WaitGroup
	var firstErr error

	for _, scenario := range req.Scenarios {
		scenario := scenario
		wg.Add(1)
		go func() {
			defer wg.Done()
			nodes, err := s.generateSkeleton(ctx, scenario.BusinessGoal, "", scenario)
			mu.Lock()
			defer mu.Unlock()
			if err != nil {
				if firstErr == nil {
					firstErr = err
				}
			} else {
				resp.Skeletons[scenario.ScenarioID] = nodes
			}
		}()
	}
	wg.Wait()
	
	if len(resp.Skeletons) == 0 && firstErr != nil {
		return resp, firstErr
	}
	return resp, nil
}

func (s *Service) RefineCases(ctx context.Context, req model.RefineCasesRequest) (model.RefineCasesResponse, error) {
	resp := model.RefineCasesResponse{}
	
	var wg sync.WaitGroup
	var mu sync.Mutex

	for scenarioID, skeleton := range req.Skeletons {
		scenarioID := scenarioID
		skeleton := skeleton
		wg.Add(1)
		
		go func() {
			defer wg.Done()
			
			// Try to find the agentIndex from the skeleton
			agentIndex := 1
			if len(skeleton) > 0 {
				fmt.Sscanf(skeleton[0].NodeID, "Agent%d_Root", &agentIndex)
			}
			
			cases, err := s.refineNodes(ctx, req.Operation, skeleton)
			res := model.AgentResult{
				AgentIndex: agentIndex,
			}
			
			if err != nil {
				res.Errors = append(res.Errors, err.Error())
			} else {
				if issues := validator.ValidateCases(cases, skeleton, agentIndex); len(issues) > 0 {
					res.Errors = append(res.Errors, prefixIssues("case validation", issues)...)
				} else {
					outDir, err := s.store.SaveAgentCases(req.Operation, agentIndex, cases)
					if err != nil {
						res.Errors = append(res.Errors, err.Error())
					} else {
						res.NodeCount = len(cases)
						res.OutputDir = outDir
					}
				}
			}
			
			mu.Lock()
			resp.Agents = append(resp.Agents, res)
			if len(res.Errors) == 0 {
				resp.TotalSaved += len(cases)
			}
			mu.Unlock()
		}()
	}
	wg.Wait()
	
	return resp, nil
}
