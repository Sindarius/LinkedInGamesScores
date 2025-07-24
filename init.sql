-- Create database if it doesn't exist
SELECT 'CREATE DATABASE gamescores'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'gamescores');

-- Connect to the gamescores database
\c gamescores;

-- The Entity Framework migrations will handle table creation
-- This file is just to ensure the database exists