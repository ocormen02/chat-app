export interface AuthResponse {
  token: string;
  userId: string;
  email: string;
  displayName?: string;
}

export interface RegisterDto {
  email: string;
  password: string;
  displayName?: string;
}

export interface LoginDto {
  email: string;
  password: string;
}

export interface Chatroom {
  id: number;
  name: string;
  description?: string;
  createdAt: string;
}

export interface Message {
  id: number;
  userId: string;
  userName?: string;
  userDisplayName?: string;
  chatroomId: number;
  content: string;
  timestamp: string;
  isBotMessage: boolean;
}

export interface SendMessageDto {
  chatroomId: number;
  content: string;
}
