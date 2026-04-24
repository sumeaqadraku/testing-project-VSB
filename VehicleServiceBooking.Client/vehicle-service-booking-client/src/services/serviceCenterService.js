import api from './api';

export const serviceCenterService = {
  async getAll() {
    const response = await api.get('/ServiceCentersApi');
    return response.data;
  },

  async getById(id) {
    const response = await api.get(`/ServiceCentersApi/${id}`);
    return response.data;
  },

  async create(center) {
    const response = await api.post('/ServiceCentersApi', center);
    return response.data;
  },

  async update(id, center) {
    await api.put(`/ServiceCentersApi/${id}`, center);
  },

  async delete(id) {
    await api.delete(`/ServiceCentersApi/${id}`);
  },
};

