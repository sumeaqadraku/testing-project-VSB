import api from './api';

export const scheduleService = {
  async getAll(mechanicId) {
    const url = mechanicId ? `/SchedulesApi?mechanicId=${mechanicId}` : '/SchedulesApi';
    const response = await api.get(url);
    return response.data;
  },

  async getById(id) {
    const response = await api.get(`/SchedulesApi/${id}`);
    return response.data;
  },

  async create(schedule) {
    const response = await api.post('/SchedulesApi', schedule);
    return response.data;
  },

  async update(id, schedule) {
    await api.put(`/SchedulesApi/${id}`, schedule);
  },

  async delete(id) {
    await api.delete(`/SchedulesApi/${id}`);
  },
};

