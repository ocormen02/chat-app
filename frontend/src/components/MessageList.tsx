import React, { useEffect, useRef } from 'react';
import {
  Box,
  List,
  ListItem,
  ListItemAvatar,
  Avatar,
  Typography,
  Chip,
  Paper,
} from '@mui/material';
import { Message } from '../types';
import BotIcon from '@mui/icons-material/SmartToy';

interface MessageListProps {
  messages: Message[];
}

const MessageList: React.FC<MessageListProps> = ({ messages }) => {
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp);
    return date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
  };

  return (
    <Box sx={{ height: '100%', overflow: 'auto', bgcolor: 'background.default' }}>
      <List sx={{ p: 1 }}>
        {messages.map((message) => (
          <ListItem
            key={message.id}
            sx={{
              flexDirection: message.isBotMessage ? 'row' : 'row-reverse',
              alignItems: 'flex-start',
              mb: 1,
            }}
          >
            <ListItemAvatar sx={{ order: message.isBotMessage ? 0 : 2 }}>
              <Avatar sx={{ bgcolor: message.isBotMessage ? 'secondary.main' : 'primary.main' }}>
                {message.isBotMessage ? (
                  <BotIcon />
                ) : (
                  (message.userDisplayName || message.userName || 'U').charAt(0).toUpperCase()
                )}
              </Avatar>
            </ListItemAvatar>
            <Paper
              elevation={1}
              sx={{
                p: 1.5,
                maxWidth: '70%',
                bgcolor: message.isBotMessage ? 'grey.100' : 'primary.light',
                color: message.isBotMessage ? 'text.primary' : 'primary.contrastText',
                borderRadius: 2,
              }}
            >
              <Box display="flex" alignItems="center" gap={1} mb={0.5}>
                <Typography variant="subtitle2" fontWeight="bold">
                  {message.isBotMessage
                    ? 'StockBot'
                    : message.userDisplayName || message.userName || 'Unknown User'}
                </Typography>
                <Chip
                  label={formatTimestamp(message.timestamp)}
                  size="small"
                  sx={{ height: 20, fontSize: '0.7rem' }}
                />
              </Box>
              <Typography variant="body1">{message.content}</Typography>
            </Paper>
          </ListItem>
        ))}
        <div ref={messagesEndRef} />
      </List>
    </Box>
  );
};

export default MessageList;
