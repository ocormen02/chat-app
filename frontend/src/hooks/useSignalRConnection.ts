import { useEffect, useState, useRef } from 'react';
import signalRService from '../services/signalrService';

interface UseSignalRConnectionResult {
  connected: boolean;
  error: string;
}

export const useSignalRConnection = (chatroomId: number): UseSignalRConnectionResult => {
  const [connected, setConnected] = useState(false);
  const [error, setError] = useState('');
  const isMountedRef = useRef(true);

  useEffect(() => {
    isMountedRef.current = true;

    const connectToSignalR = async () => {
      try {

        // Start connection
        await signalRService.startConnection();

        if (!isMountedRef.current) {          
          return;
        }

        // Verify connection is ready
        signalRService.ensureConnected();       

        if (!isMountedRef.current) return;

        // Join chatroom
        await signalRService.joinChatroom(chatroomId);      

        if (!isMountedRef.current) return;

        // Mark as connected
        setConnected(true);
        setError('');
      } catch (err: any) {
        console.error('[useSignalRConnection] Connection error:', err);
        if (isMountedRef.current) {
          const errorMessage = err?.message || 'Failed to connect to SignalR';
          setError(errorMessage);
          setConnected(false);
        }
      }
    };

    connectToSignalR();

    // Cleanup function
    return () => {
      isMountedRef.current = false;
      signalRService.leaveChatroom(chatroomId);
    };
  }, [chatroomId]);

  return { connected, error };
};
