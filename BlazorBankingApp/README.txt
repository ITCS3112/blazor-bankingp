Our Project uses supabase and postgreSQL so there are some packages that need to be installed to run the application:
	
Supabase package: 
dotnet add package Supabase --version 1.1.1

PostgreSQL:
dotnet add package Npgsql --version 9.0.3

DotNetEnv: 
dotnet add package DotNetEnv --version 3.1.1

The supabase package allows you to execute and write commands that interact with the supabase database. The Npgsql allows you to write sql commands in code to better format our code and run these commands. The DotNetEnv package lets us use a .env file to have environment variables that we can use
