import axios, { AxiosInstance } from 'axios';
import { AuthResponse, RegisterDto, LoginDto, Chatroom, Message } from '../types';

// Always use relative URL '/api' to leverage Vite proxy in development
// This ensures all requests go through the proxy and maintain authentication tokens
const API_BASE_URL = '/api';


const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor to add token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    } else {
      console.warn('[API Request] No token found in localStorage for request:', config.url);
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor to handle errors
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('authToken');
      localStorage.removeItem('userId');
      localStorage.removeItem('userEmail');
      localStorage.removeItem('userDisplayName');
    }
    return Promise.reject(error);
  }
);

export const apiService = {
  // Auth endpoints
  async register(data: RegisterDto): Promise<AuthResponse> {
    const response = await apiClient.post<AuthResponse>('/auth/register', data);
    return response.data;
  },

  async login(data: LoginDto): Promise<AuthResponse> {
    const response = await apiClient.post<AuthResponse>('/auth/login', data);
    return response.data;
  },

  async logout(): Promise<void> {
    await apiClient.post('/auth/logout');
  },

  // Chatroom endpoints
  async getChatrooms(): Promise<Chatroom[]> {
    const response = await apiClient.get<Chatroom[]>('/chatrooms');
    return response.data;
  },

  async getChatroom(id: number): Promise<Chatroom> {
    const response = await apiClient.get<Chatroom>(`/chatrooms/${id}`);
    return response.data;
  },

  async createChatroom(name: string, description?: string): Promise<Chatroom> {
    const response = await apiClient.post<Chatroom>('/chatrooms', { name, description });
    return response.data;
  },

  // Message endpoints
  async getMessages(chatroomId: number, limit: number = 50): Promise<Message[]> {
    const response = await apiClient.get<Message[]>(`/chatrooms/${chatroomId}/messages`, {
      params: { limit },
    });
    return response.data;
  },
};

export default apiService;
