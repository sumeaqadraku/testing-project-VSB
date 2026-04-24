import api from './api';

export const serviceTypeService = {
  async getAll(activeOnly = false) {
    const response = await api.get(`/ServiceTypesApi?activeOnly=${activeOnly}`);
    return response.data;
  },

  async getById(id) {
    const response = await api.get(`/ServiceTypesApi/${id}`);
    return response.data;
  },

  async create(serviceType) {
    const response = await api.post('/ServiceTypesApi', serviceType);
    return response.data;
  },

  async update(id, serviceType) {
    await api.put(`/ServiceTypesApi/${id}`, serviceType);
  },

  async delete(id) {
    await api.delete(`/ServiceTypesApi/${id}`);
  },
};

