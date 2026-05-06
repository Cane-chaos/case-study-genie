from fastapi import APIRouter, Depends, HTTPException, status
from pydantic import BaseModel, EmailStr
import bcrypt
from datetime import datetime, timedelta
from jose import jwt
from motor.motor_asyncio import AsyncIOMotorDatabase
from api_casestudy.db.database import get_database
from api_casestudy.core.config import settings

router = APIRouter()

# Pydantic Schemas
class UserBase(BaseModel):
    email: EmailStr

class UserCreate(UserBase):
    password: str

class Token(BaseModel):
    access_token: str
    token_type: str

# Helper functions sử dụng bcrypt trực tiếp
def get_password_hash(password: str) -> str:
    """Mã hóa mật khẩu bằng bcrypt (trả về chuỗi string)"""
    pwd_bytes = password.encode('utf-8')
    salt = bcrypt.gensalt()
    hashed = bcrypt.hashpw(pwd_bytes, salt)
    return hashed.decode('utf-8')

def verify_password(plain_password: str, hashed_password: str) -> bool:
    """Kiểm tra mật khẩu thô so với mật khẩu đã mã hóa"""
    password_byte_enc = plain_password.encode('utf-8')
    hashed_password_enc = hashed_password.encode('utf-8')
    try:
        return bcrypt.checkpw(password_byte_enc, hashed_password_enc)
    except Exception:
        return False

def create_access_token(data: dict):
    to_encode = data.copy()
    expire = datetime.utcnow() + timedelta(minutes=settings.ACCESS_TOKEN_EXPIRE_MINUTES)
    to_encode.update({"exp": expire})
    encoded_jwt = jwt.encode(to_encode, settings.JWT_SECRET, algorithm=settings.ALGORITHM)
    return encoded_jwt

# === REGISTER ===
@router.post("/register", status_code=status.HTTP_201_CREATED)
async def register(user_data: UserCreate, db: AsyncIOMotorDatabase = Depends(get_database)):
    # 1. Kiểm tra email tồn tại (Collection 'member' theo ý bạn)
    existing_user = await db["member"].find_one({"email": user_data.email})
    if existing_user:
        raise HTTPException(status_code=400, detail="Email already registered")
    
    # 2. Tạo bản ghi người dùng mới
    new_user = {
        "email": user_data.email,
        "password": get_password_hash(user_data.password),
        "created_at": datetime.utcnow(),
        "updated_at": datetime.utcnow()
    }
    
    result = await db["member"].insert_one(new_user)
    return {"id": str(result.inserted_id), "message": "Registered successfully"}

# === LOGIN ===
@router.post("/login", response_model=Token)
async def login(user_data: UserCreate, db: AsyncIOMotorDatabase = Depends(get_database)):
    # 1. Tìm người dùng
    user = await db["member"].find_one({"email": user_data.email})
    if not user:
        raise HTTPException(status_code=400, detail="Incorrect email or password")
    
    # 2. Kiểm tra mật khẩu
    if not verify_password(user_data.password, user["password"]):
        raise HTTPException(status_code=400, detail="Incorrect email or password")
    
    # 3. Tạo Token
    access_token = create_access_token(data={"sub": user["email"]})
    return {"access_token": access_token, "token_type": "bearer"}
