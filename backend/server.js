require('dotenv').config();
const express = require('express');
const cors = require('cors');
const emailRoutes = require('./routes/email');

const app = express();
const PORT = process.env.BACKEND_PORT || 3001;

// Middleware
app.use(cors({
  origin: process.env.FRONTEND_URL || 'http://localhost:3000', // Adjust for your frontend URL
}));
app.use(express.json());



// Routes
app.use('/api/email', emailRoutes); // All email routes will be prefixed with /api/email

app.get('/api/health', (req, res) => { // Health check endpoint
    res.status(200).json({ status: 'UP', message: 'Backend is running' });
});

app.listen(PORT, () => {
  console.log(`Backend server running on port ${PORT}`);
});