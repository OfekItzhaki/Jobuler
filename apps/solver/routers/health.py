from fastapi import APIRouter
from datetime import datetime, timezone

router = APIRouter()

@router.get("/health")
def health_check():
    return {"status": "healthy", "utc": datetime.now(timezone.utc).isoformat()}
