@echo off
curl -s -X POST http://localhost:5000/auth/login -H "Content-Type: application/json" -d "{\"email\":\"admin@demo.local\",\"password\":\"Demo1234!\"}"
echo.
