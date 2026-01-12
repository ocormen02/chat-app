import React, { useState } from 'react';
import { Box, Grid, Container } from '@mui/material';
import ChatroomList from './ChatroomList';
import ChatWindow from './ChatWindow';
import apiService from '../services/apiService';

const ChatroomsPage: React.FC = () => {
  const [selectedChatroomId, setSelectedChatroomId] = useState<number | undefined>();
  const [selectedChatroomName, setSelectedChatroomName] = useState<string>('');

  const handleSelectChatroom = async (chatroomId: number) => {
    setSelectedChatroomId(chatroomId);

    try {
      const chatroom = await apiService.getChatroom(chatroomId);
      setSelectedChatroomName(chatroom.name);
    } catch (err) {
      setSelectedChatroomName('Chatroom');
    }
  };

  if (selectedChatroomId) {
    return (
      <ChatWindow chatroomId={selectedChatroomId} chatroomName={selectedChatroomName} />
    );
  }

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Grid container spacing={3}>
        <Grid item xs={12} md={4}>
          <ChatroomList
            onSelectChatroom={handleSelectChatroom}
            selectedChatroomId={selectedChatroomId}
          />
        </Grid>
        <Grid item xs={12} md={8}>
          <Box
            sx={{
              height: '100%',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              bgcolor: 'background.paper',
              borderRadius: 2,
              p: 4,
            }}
          >
            <Box textAlign="center">
              <h2>Welcome to ChatApp</h2>
              <p>Select a chatroom from the list to start chatting, or create a new one!</p>
              <p>Use <code>/stock=CODE</code> to get stock quotes (e.g., /stock=AAPL.US)</p>
            </Box>
          </Box>
        </Grid>
      </Grid>
    </Container>
  );
};

export default ChatroomsPage;
