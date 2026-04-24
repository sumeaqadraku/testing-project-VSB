import api from './api';

export const mechanicService = {
  async getAll() {
    const response = await api.get('/MechanicsApi');
    return response.data;
  },

  async getById(id) {
    const response = await api.get(`/MechanicsApi/${id}`);
    return response.data;
  },

  async create(mechanic) {
    const response = await api.post('/MechanicsApi', mechanic);
    return response.data;
  },

  async update(id, mechanic) {
    await api.put(`/MechanicsApi/${id}`, mechanic);
  },

  async delete(id) {
    await api.delete(`/MechanicsApi/${id}`);
  },
};

