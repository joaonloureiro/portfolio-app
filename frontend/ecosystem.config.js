module.exports = {
  apps: [{
    name: 'portfolio-frontend',  // A name for your application in PM2
    script: 'npm',               // Tell PM2 to run an npm command
    args: 'start',               // The argument to pass to npm, which runs 'next start'
    instances: 1,                // Run a single instance of the application
    autorestart: true,           // Automatically restart if the app crashes
    watch: false,                // Do NOT watch for file changes in production
    max_memory_restart: '512M',  // Restart if it uses more than 512MB of RAM
    env: {
      NODE_ENV: 'production',    // Set the environment to production
    }
  }]
};