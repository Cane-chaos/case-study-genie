from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from api_casestudy.core.config import settings
from api_casestudy.db.database import connect_to_mongo, close_mongo_connection
from api_casestudy.routers import auth

def create_app() -> FastAPI:
    """Khởi tạo ứng dụng FastAPI"""
    app = FastAPI(
        title=settings.PROJECT_NAME,
        version=settings.VERSION,
        description="Backend chính xử lý Auth, Case Management và Agent Coordination."
    )

    # Cấu hình CORS (Cho phép Frontend gọi API)
    app.add_middleware(
        CORSMiddleware,
        allow_origins=["*"],
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )

    # Sự kiện khởi động và đóng ứng dụng
    @app.on_event("startup")
    async def startup_event():
        await connect_to_mongo()

    @app.on_event("shutdown")
    async def shutdown_event():
        await close_mongo_connection()

    # Đăng ký các Router (Auth, Assets, etc.)
    app.include_router(auth.router, prefix="/api", tags=["Auth"])

    @app.get("/healthz")
    async def health_check():
        return {"status": "ok", "project": settings.PROJECT_NAME}

    return app

app = create_app()

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("api_casestudy.main:app", host="0.0.0.0", port=8001, reload=True)
