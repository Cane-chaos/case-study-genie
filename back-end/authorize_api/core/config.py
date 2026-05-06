from pydantic_settings import BaseSettings
from typing import Optional
import os
from dotenv import load_dotenv

load_dotenv()

class Settings(BaseSettings):
    PROJECT_NAME: str = "CaseStudy Agent API"
    VERSION: str = "1.0.0"
    
    # MongoDB Configuration
    MONGO_URI: str = os.getenv("MONGO_URI", "mongodb+srv://felixthinkai_db_user:5812@casestudy.ibyfg72.mongodb.net/?appName=CaseStudy")
    DATABASE_NAME: str = "CaseStudy"
    
    # JWT Configuration
    JWT_SECRET: str = os.getenv("JWT_SECRET", "super_secret_key_change_me")
    ALGORITHM: str = "HS256"
    ACCESS_TOKEN_EXPIRE_MINUTES: int = 60 * 24 * 30  # 30 days

    # OpenAI Configuration
    OPENAI_API_KEY: str = os.getenv("OPENAI_API_KEY", "")
    OPENAI_MODEL: str = os.getenv("OPENAI_MODEL", "gpt-4o-mini")

    class Config:
        case_sensitive = True

settings = Settings()
