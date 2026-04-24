import api from './api';

export const partService = {
  async getAll() {
    const response = await api.get('/PartsApi');
    return response.data;
  },

  async getById(id) {
    const response = await api.get(`/PartsApi/${id}`);
    return response.data;
  },

  async create(part) {
    const response = await api.post('/PartsApi', part);
    return response.data;
  },

  async update(id, part) {
    await api.put(`/PartsApi/${id}`, part);
  },

  async delete(id) {
    await api.delete(`/PartsApi/${id}`);
  },
};

