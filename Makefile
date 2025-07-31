
all: build

# Builds the mod using dotnet
# Read the README.md for more info
build:
	dotnet build .vscode

workspace:
	rmdir .github -Force
	rmdir .vscode -Force
	rmdir .git -Force
	rm Makefile -Force
	rm .gitignore -Force
