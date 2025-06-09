const express = require('express');
const nodemailer = require('nodemailer');
const router = express.Router();

router.post('/send', async (req, res) => {
  const { name, email, message } = req.body;

  if (!name ||!email ||!message) {
    return res.status(400).json({ message: 'All fields are required.' });
  }
  if (!/\S+@\S+\.\S+/.test(email)) {
    return res.status(400).json({ message: 'Invalid email address.' });
  }

  const transporter = nodemailer.createTransport({
    host: process.env.SMTP_HOST,
    port: parseInt(process.env.SMTP_PORT || '587', 10),
    secure: process.env.SMTP_SECURE === 'true', // true for 465, false for other ports
    auth: {
    user: process.env.SMTP_USER,
    pass: process.env.SMTP_PASS,
  },
// If using self-signed certificates or having issues with TLS:
// tls: {
// rejectUnauthorized: false // Use with caution, only for development/testing
// }
});

const mailOptions = {
  from: `"${name}" <${process.env.SMTP_FROM_EMAIL || process.env.SMTP_USER}>`, // Use a configured FROM email or fallback
  replyTo: email,
  to: process.env.YOUR_RECEIVING_EMAIL, // Your email address to receive submissions
  subject: `New Portfolio Contact: ${name}`,
  text: `Name: ${name}\nEmail: ${email}\nMessage: ${message}`,
  html: `<p><strong>Name:</strong> ${name}</p>
         <p><strong>Email:</strong> <a href="mailto:${email}">${email}</a></p>
         <p><strong>Message:</strong></p>
         <p>${message.replace(/\n/g, '<br>')}</p>`,
};



  try {
    await transporter.sendMail(mailOptions);
    res.status(200).json({ message: 'Message sent successfully!' });
  } catch (error) {
    console.error('Error sending email:', error);
    // Provide a more generic error message to the client
    res.status(500).json({ message: 'Failed to send message. Please try again later.' });
  }
});

module.exports = router;