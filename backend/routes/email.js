const express = require('express');
const nodemailer = require('nodemailer');
const router = express.Router();

router.post('/send', async (req, res) => {
  const { name, email, message } = req.body;

  if (!name || !email || !message) {
    return res.status(400).json({ errorKey: 'status_error_all_fields' });
  }
  if (!/\S+@\S+\.\S+/.test(email)) {
    return res.status(400).json({ errorKey: 'status_error_invalid_email' });
  }

  const transporter = nodemailer.createTransport({
    host: process.env.SMTP_HOST,
    port: parseInt(process.env.SMTP_PORT || '587', 10),
    secure: process.env.SMTP_SECURE === 'true',
    auth: {
      user: process.env.SMTP_USER,
      pass: process.env.SMTP_PASS,
    },
  });

  const mailOptions = {
    from: `"${name}" <${process.env.SMTP_FROM_EMAIL || process.env.SMTP_USER}>`,
    replyTo: email,
    to: process.env.YOUR_RECEIVING_EMAIL,
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
    if (error.code) {
      switch (error.code) {
        case 'EAUTH':
          return res.status(500).json({ errorKey: 'smtp_auth_failed' });
        case 'ECONNECTION':
          return res.status(500).json({ errorKey: 'smtp_connection_failed' });
        case 'EENVELOPE':
          return res.status(400).json({ errorKey: 'smtp_invalid_recipient' });
        default:
          return res.status(500).json({ errorKey: 'smtp_generic_error' });
      }
    }

    res.status(500).json({ errorKey: 'server_unexpected_error' });
  }
});

module.exports = router;