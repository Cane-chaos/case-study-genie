from motor.motor_asyncio import AsyncIOMotorClient
from api_casestudy.core.config import settings
import logging

class Database:
    client: AsyncIOMotorClient = None
    db = None

db = Database()

async def connect_to_mongo():
    """Khởi tạo kết nối tới MongoDB"""
    try:
        db.client = AsyncIOMotorClient(settings.MONGO_URI)
        db.db = db.client[settings.DATABASE_NAME]
        # Kiểm tra kết nối
        await db.client.admin.command('ping')
        print(f"✅ Successfully connected to MongoDB Atlas (DB: {settings.DATABASE_NAME})")
    except Exception as e:
        logging.error(f"❌ Failed to connect to MongoDB: {e}")
        raise e

async def close_mongo_connection():
    """Đóng kết nối MongoDB"""
    if db.client:
        db.client.close()
        print("🛑 MongoDB connection closed")

def get_database():
    """Lấy đối tượng database hiện tại"""
    return db.db
