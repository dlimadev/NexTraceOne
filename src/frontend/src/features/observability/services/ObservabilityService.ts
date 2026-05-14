import axios from 'axios';
import type { RequestMetrics, ErrorAnalytics, UserActivityMetrics, SystemHealthMetrics, DashboardFilters } from '../types/ObservabilityTypes';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export class ObservabilityService {
  private static instance: ObservabilityService;

  private constructor() {}

  public static getInstance(): ObservabilityService {
    if (!ObservabilityService.instance) {
      ObservabilityService.instance = new ObservabilityService();
    }
    return ObservabilityService.instance;
  }

  async getRequestMetrics(filters: DashboardFilters): Promise<RequestMetrics[]> {
    try {
      const params = this.buildQueryParams(filters);
      const response = await axios.get(`${API_BASE_URL}/api/v1/observability/request-metrics`, { params });
      return response.data;
    } catch (error) {
      console.error('Error fetching request metrics:', error);
      throw error;
    }
  }

  async getErrorAnalytics(filters: DashboardFilters): Promise<ErrorAnalytics[]> {
    try {
      const params = this.buildQueryParams(filters);
      const response = await axios.get(`${API_BASE_URL}/api/v1/observability/error-analytics`, { params });
      return response.data;
    } catch (error) {
      console.error('Error fetching error analytics:', error);
      throw error;
    }
  }

  async getUserActivity(filters: DashboardFilters): Promise<UserActivityMetrics[]> {
    try {
      const params = this.buildQueryParams(filters);
      const response = await axios.get(`${API_BASE_URL}/api/v1/observability/user-activity`, { params });
      return response.data;
    } catch (error) {
      console.error('Error fetching user activity:', error);
      throw error;
    }
  }

  async getSystemHealth(filters: DashboardFilters): Promise<SystemHealthMetrics[]> {
    try {
      const params = this.buildQueryParams(filters);
      const response = await axios.get(`${API_BASE_URL}/api/v1/observability/system-health`, { params });
      return response.data;
    } catch (error) {
      console.error('Error fetching system health:', error);
      throw error;
    }
  }

  async getOverallStats(filters: DashboardFilters): Promise<{
    totalRequests: number;
    avgResponseTime: number;
    errorRate: number;
    activeUsers: number;
  }> {
    try {
      const params = this.buildQueryParams(filters);
      const response = await axios.get(`${API_BASE_URL}/api/v1/observability/stats`, { params });
      return response.data;
    } catch (error) {
      console.error('Error fetching overall stats:', error);
      throw error;
    }
  }

  private buildQueryParams(filters: DashboardFilters): any {
    const now = new Date();
    let from: Date;

    switch (filters.timeRange) {
      case '1h':
        from = new Date(now.getTime() - 60 * 60 * 1000);
        break;
      case '6h':
        from = new Date(now.getTime() - 6 * 60 * 60 * 1000);
        break;
      case '24h':
        from = new Date(now.getTime() - 24 * 60 * 60 * 1000);
        break;
      case '7d':
        from = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
        break;
      case '30d':
        from = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
        break;
      default:
        from = new Date(now.getTime() - 24 * 60 * 60 * 1000);
    }

    return {
      from: from.toISOString(),
      to: now.toISOString(),
      serviceName: filters.serviceName,
      endpoint: filters.endpoint,
      environment: filters.environment
    };
  }
}

export const observabilityService = ObservabilityService.getInstance();
