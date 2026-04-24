@echo off
git add -A
git commit -m "fix(api): rename TransferOwnershipRequest in GroupsController to avoid duplicate with SpacesController"
git push origin main
