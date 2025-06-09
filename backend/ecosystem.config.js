module.exports = {
  apps: [{
    name: 'portfolio-backend', // A name for your application in PM2
    script: 'backend/server.js',      // The path to the script PM2 will run
    instances: 1,             // Run a single instance of the application
    autorestart: true,        // Automatically restart if the app crashes
    watch: false,             // Do NOT watch for file changes in production
    max_memory_restart: '256M', // Restart if it uses more than 256MB of RAM
    env: {
      NODE_ENV: 'production', // Set the environment to production
    }
  }]
};