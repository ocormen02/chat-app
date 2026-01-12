import React, { useCallback } from 'react';
import {
  Box,
  Container,
  Typography,
  Alert,
  CircularProgress,
  AppBar,
  Toolbar,
  IconButton,
} from '@mui/material';
import LogoutIcon from '@mui/icons-material/Logout';
import { useNavigate } from 'react-router-dom';
import signalRService from '../services/signalrService';
import authService from '../services/authService';
import { useSignalRConnection } from '../hooks/useSignalRConnection';
import { useChatMessages } from '../hooks/useChatMessages';
import MessageList from './MessageList';
import MessageInput from './MessageInput';

interface ChatWindowProps {
  chatroomId: number;
  chatroomName: string;
}

const ChatWindow: React.FC<ChatWindowProps> = ({ chatroomId, chatroomName }) => {
  const navigate = useNavigate();
  
  const { connected, error: signalRError } = useSignalRConnection(chatroomId);
  const { messages, loading, error: messagesError } = useChatMessages(chatroomId, connected);

  const error = signalRError || messagesError;

  const handleSendMessage = useCallback(async (content: string) => {
    try {
      await signalRService.sendMessage(chatroomId, content);
    } catch (err: any) {
      console.error('Error sending message:', err);
    }
  }, [chatroomId]);

  const handleLogout = useCallback(() => {
    authService.clearAuthData();
    signalRService.stopConnection();
    navigate('/login');
  }, [navigate]);

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" height="100vh">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100vh' }}>
      <AppBar position="static">
        <Toolbar>
          <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
            {chatroomName}
          </Typography>
          <IconButton color="inherit" onClick={handleLogout}>
            <LogoutIcon />
          </IconButton>
        </Toolbar>
      </AppBar>

      <Container maxWidth="lg" sx={{ flex: 1, display: 'flex', flexDirection: 'column', py: 2 }}>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => {}}>
            {error}
          </Alert>
        )}
        {!connected && (
          <Alert severity="warning" sx={{ mb: 2 }}>
            Connecting to chat...
          </Alert>
        )}

        <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', mb: 2 }}>
          <MessageList messages={messages} />
        </Box>

        <MessageInput onSendMessage={handleSendMessage} disabled={!connected} />
      </Container>
    </Box>
  );
};

export default ChatWindow;
