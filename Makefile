.PHONY: build run test clean migrate up down

# Build the project
build:
	cd prReviewerAppoint && dotnet build

# Run the project locally (requires PostgreSQL running)
run:
	cd prReviewerAppoint && dotnet run

# Run tests
test:
	cd prReviewerAppoint.Tests && dotnet test

# Clean build artifacts
clean:
	cd prReviewerAppoint && dotnet clean
	rm -rf prReviewerAppoint/bin prReviewerAppoint/obj

# Create a new migration
migrate:
	cd prReviewerAppoint && dotnet ef migrations add InitialCreate --output-dir Migrations

# Apply migrations
migrate-apply:
	cd prReviewerAppoint && dotnet ef database update

# Start services with docker-compose
up:
	cd prReviewerAppoint && docker-compose up --build

# Stop services
down:
	cd prReviewerAppoint && docker-compose down

# Restart services
restart: down up

