#!/bin/bash

# Check if the container exists
if [ "$(docker ps -aq -f name=sql1)" ]; then
    # Stop and remove the container if it exists
    docker rm -f sql1
    echo "Removed existing sql1 container."
fi

# Run the new container
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=test123@321" -p 1433:1433 --name sql1 --hostname sql1 -d mcr.microsoft.com/mssql/server:2022-latest

# Wait for SQL Server to start
echo "Waiting for SQL Server to start..."
sleep 10


# Create the database

docker exec sql1 bash -c "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'test123@321' -C -Q 'CREATE DATABASE rockdatablydev'"

echo "Database rockdatablydev created."