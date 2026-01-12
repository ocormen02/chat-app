import React, { useEffect, useState } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
  Typography,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  CircularProgress,
  Alert,
} from '@mui/material';
import { Chatroom } from '../types';
import apiService from '../services/apiService';

interface ChatroomListProps {
  onSelectChatroom: (chatroomId: number) => void;
  selectedChatroomId?: number;
}

const ChatroomList: React.FC<ChatroomListProps> = ({ onSelectChatroom, selectedChatroomId }) => {
  const [chatrooms, setChatrooms] = useState<Chatroom[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [openDialog, setOpenDialog] = useState(false);
  const [newChatroomName, setNewChatroomName] = useState('');
  const [newChatroomDescription, setNewChatroomDescription] = useState('');
  const [creating, setCreating] = useState(false);

  useEffect(() => {
    loadChatrooms();
  }, []);

  const loadChatrooms = async () => {
    try {
      setLoading(true);
      const data = await apiService.getChatrooms();
      setChatrooms(data);
    } catch (err: any) {
      setError('Failed to load chatrooms');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateChatroom = async () => {
    if (!newChatroomName.trim()) {
      return;
    }

    try {
      setCreating(true);
      const newChatroom = await apiService.createChatroom(
        newChatroomName.trim(),
        newChatroomDescription.trim() || undefined
      );
      setChatrooms([...chatrooms, newChatroom]);
      setOpenDialog(false);
      setNewChatroomName('');
      setNewChatroomDescription('');
      onSelectChatroom(newChatroom.id);
    } catch (err: any) {
      setError('Failed to create chatroom');
    } finally {
      setCreating(false);
    }
  };

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" p={2}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Card>
      <CardContent>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
          <Typography variant="h6">Chatrooms</Typography>
          <Button variant="contained" size="small" onClick={() => setOpenDialog(true)}>
            New
          </Button>
        </Box>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>
            {error}
          </Alert>
        )}
        <List>
          {chatrooms.map((chatroom) => (
            <ListItem key={chatroom.id} disablePadding>
              <ListItemButton
                selected={selectedChatroomId === chatroom.id}
                onClick={() => onSelectChatroom(chatroom.id)}
              >
                <ListItemText
                  primary={chatroom.name}
                  secondary={chatroom.description || 'No description'}
                />
              </ListItemButton>
            </ListItem>
          ))}
        </List>
        {chatrooms.length === 0 && (
          <Typography variant="body2" color="text.secondary" align="center" sx={{ mt: 2 }}>
            No chatrooms available. Create one to get started!
          </Typography>
        )}
      </CardContent>

      <Dialog open={openDialog} onClose={() => setOpenDialog(false)}>
        <DialogTitle>Create New Chatroom</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            margin="dense"
            label="Chatroom Name"
            fullWidth
            variant="outlined"
            value={newChatroomName}
            onChange={(e) => setNewChatroomName(e.target.value)}
            sx={{ mb: 2 }}
          />
          <TextField
            margin="dense"
            label="Description (optional)"
            fullWidth
            variant="outlined"
            multiline
            rows={3}
            value={newChatroomDescription}
            onChange={(e) => setNewChatroomDescription(e.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenDialog(false)}>Cancel</Button>
          <Button onClick={handleCreateChatroom} variant="contained" disabled={creating || !newChatroomName.trim()}>
            {creating ? 'Creating...' : 'Create'}
          </Button>
        </DialogActions>
      </Dialog>
    </Card>
  );
};

export default ChatroomList;
