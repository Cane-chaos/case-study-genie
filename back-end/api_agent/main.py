import uvicorn
import os
from fastapi import FastAPI
from fastapi.staticfiles import StaticFiles
from api_agent.routers import admin_router, game_router

app = FastAPI(
    title="CaseStudy Engine - Agent API",
    description="API for LangGraph Multi-Agent Simulation and Case Generation",
    version="1.0.0"
)

# Nạp các router
app.include_router(admin_router.router, prefix="/api")
app.include_router(game_router.router, prefix="/api")

# Phục vụ Front-end Tree Viewer
viewer_dir = os.path.join(os.path.dirname(os.path.dirname(__file__)), "tree_viewer")
os.makedirs(viewer_dir, exist_ok=True)
app.mount("/viewer", StaticFiles(directory=viewer_dir, html=True), name="viewer")

@app.get("/")
def read_root():
    return {"message": "Welcome to CaseStudy Engine - Agent API (Port 9000)"}

if __name__ == "__main__":
    # Cấu hình để chạy server tại port 9000 (theo file GEMINI.md)
    uvicorn.run("api_agent.main:app", host="0.0.0.0", port=9000, reload=True)
