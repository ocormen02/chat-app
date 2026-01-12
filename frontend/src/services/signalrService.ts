import * as signalR from '@microsoft/signalr';
import { Message } from '../types';

class SignalRService {
  private connection: signalR.HubConnection | null = null;

  async startConnection(): Promise<void> {
    // If connection exists and is already connected, return immediately
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return;
    }

    // If connection exists but is not connected, start it (start() is idempotent)
    if (this.connection) {
      try {
        await this.connection.start();
        return;
      } catch (error) {
        console.error('Error starting existing SignalR connection:', error);
        throw error;
      }
    }

    // If no connection exists, create a new one
    const token = localStorage.getItem('authToken');
    if (!token) {
      throw new Error('No authentication token found');
    }

    const hubUrl = '/chatHub';

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => token,
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    try {
      await this.connection.start();
    } catch (error) {
      console.error('Error starting SignalR connection:', error);
      throw error;
    }
  }

  async stopConnection(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }

  async joinChatroom(chatroomId: number): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      await this.startConnection();
    }
    await this.connection!.invoke('JoinChatroom', chatroomId);
  }

  async leaveChatroom(chatroomId: number): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('LeaveChatroom', chatroomId);
    }
  }

  async sendMessage(chatroomId: number, content: string): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      await this.startConnection();
    }
    await this.connection!.invoke('SendMessage', chatroomId, content);
  }

  onReceiveMessage(callback: (message: Message) => void): void {
    this.ensureConnected();
    this.connection!.on('ReceiveMessage', callback);
  }

  onReceiveMessages(callback: (messages: Message[]) => void): void {
    this.ensureConnected();
    this.connection!.on('ReceiveMessages', callback);
  }

  onError(callback: (error: string) => void): void {
    this.ensureConnected();
    this.connection!.on('Error', callback);
  }

  offReceiveMessage(callback: (message: Message) => void): void {
    if (this.connection) {
      try {
        this.connection.off('ReceiveMessage', callback);
      } catch (error) {
        console.warn('[SignalRService] Error unregistering ReceiveMessage handler:', error);
      }
    }
  }

  offReceiveMessages(callback: (messages: Message[]) => void): void {
    if (this.connection) {
      try {
        this.connection.off('ReceiveMessages', callback);
      } catch (error) {
        console.warn('[SignalRService] Error unregistering ReceiveMessages handler:', error);
      }
    }
  }

  offError(callback: (error: string) => void): void {
    if (this.connection) {
      try {
        this.connection.off('Error', callback);
      } catch (error) {
        console.warn('[SignalRService] Error unregistering Error handler:', error);
      }
    }
  }

  isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }

  ensureConnected(): void {
    if (!this.connection) {
      throw new Error('SignalR connection is null');
    }
    if (this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error(`SignalR connection is not ready. State: ${this.connection.state}`);
    }
  }
}

export const signalRService = new SignalRService();
export default signalRService;
