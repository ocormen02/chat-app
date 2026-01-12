import { useEffect, useState, useRef } from 'react';
import { Message } from '../types';
import apiService from '../services/apiService';
import signalRService from '../services/signalrService';

interface UseChatMessagesResult {
  messages: Message[];
  loading: boolean;
  error: string;
}

export const useChatMessages = (chatroomId: number, signalRConnected: boolean): UseChatMessagesResult => {
  const [messages, setMessages] = useState<Message[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  
  const handleReceiveMessageRef = useRef<((message: Message) => void) | null>(null);
  const handleReceiveMessagesRef = useRef<((messages: Message[]) => void) | null>(null);
  const handleErrorRef = useRef<((error: string) => void) | null>(null);

  // Load initial messages
  useEffect(() => {
    const loadMessages = async () => {
      try {
        setLoading(true);
        setError('');
        const data = await apiService.getMessages(chatroomId, 50);
        setMessages(data);
      } catch (err: any) {
        console.error('[useChatMessages] Error loading messages:', err);
        setError('Failed to load messages');
      } finally {
        setLoading(false);
      }
    };

    loadMessages();
  }, [chatroomId]);

  // Setup SignalR handlers when connected
  useEffect(() => {
    if (!signalRConnected) {
      return;
    }

    // Clean up previous handlers
    if (handleReceiveMessageRef.current) {
      signalRService.offReceiveMessage(handleReceiveMessageRef.current);
      handleReceiveMessageRef.current = null;
    }
    if (handleReceiveMessagesRef.current) {
      signalRService.offReceiveMessages(handleReceiveMessagesRef.current);
      handleReceiveMessagesRef.current = null;
    }
    if (handleErrorRef.current) {
      signalRService.offError(handleErrorRef.current);
      handleErrorRef.current = null;
    }

    // Create handler callbacks
    const handleReceiveMessage = (message: Message) => {
      setMessages((prev) => {
        const exists = prev.some((m) => m.id === message.id);
        if (exists) {
          return prev;
        }

        return [...prev, message];
      });
    };

    const handleReceiveMessages = (receivedMessages: Message[]) => {
      setMessages(receivedMessages);
    };

    const handleError = (errorMsg: string) => {
      console.error('[useChatMessages] SignalR error:', errorMsg);
      setError(errorMsg);
    };

    // Store handlers in refs
    handleReceiveMessageRef.current = handleReceiveMessage;
    handleReceiveMessagesRef.current = handleReceiveMessages;
    handleErrorRef.current = handleError;

    // Register handlers
    try {
      signalRService.onReceiveMessage(handleReceiveMessage);
      signalRService.onReceiveMessages(handleReceiveMessages);
      signalRService.onError(handleError);
    } catch (err: any) {
      console.error('[useChatMessages] Error registering handlers:', err);
      setError(`Failed to register message handlers: ${err?.message || 'Unknown error'}`);
    }

    // Cleanup function
    return () => {
      if (handleReceiveMessageRef.current) {
        signalRService.offReceiveMessage(handleReceiveMessageRef.current);
        handleReceiveMessageRef.current = null;
      }
      if (handleReceiveMessagesRef.current) {
        signalRService.offReceiveMessages(handleReceiveMessagesRef.current);
        handleReceiveMessagesRef.current = null;
      }
      if (handleErrorRef.current) {
        signalRService.offError(handleErrorRef.current);
        handleErrorRef.current = null;
      }
    };
  }, [signalRConnected, chatroomId]);

  return { messages, loading, error };
};
