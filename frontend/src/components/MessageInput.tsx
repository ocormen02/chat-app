import React, { useState } from 'react';
import { Box, TextField, IconButton, Paper } from '@mui/material';
import SendIcon from '@mui/icons-material/Send';

interface MessageInputProps {
  onSendMessage: (content: string) => void;
  disabled?: boolean;
}

const MessageInput: React.FC<MessageInputProps> = ({ onSendMessage, disabled = false }) => {
  const [message, setMessage] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (message.trim() && !disabled) {
      onSendMessage(message.trim());
      setMessage('');
    }
  };

  return (
    <Paper elevation={2} sx={{ p: 1 }}>
      <Box component="form" onSubmit={handleSubmit} display="flex" gap={1}>
        <TextField
          fullWidth
          variant="outlined"
          placeholder="Type a message or use /stock=CODE to get stock quotes..."
          value={message}
          onChange={(e) => setMessage(e.target.value)}
          disabled={disabled}
          size="small"
        />
        <IconButton
          type="submit"
          color="primary"
          disabled={disabled || !message.trim()}
          sx={{ alignSelf: 'flex-end' }}
        >
          <SendIcon />
        </IconButton>
      </Box>
    </Paper>
  );
};

export default MessageInput;
