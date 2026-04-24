import api from './api';

export const workOrderService = {
  async getAll() {
    const response = await api.get('/WorkOrdersApi');
    return response.data;
  },

  async getById(id) {
    const response = await api.get(`/WorkOrdersApi/${id}`);
    return response.data;
  },

  async create(workOrder) {
    const response = await api.post('/WorkOrdersApi', workOrder);
    return response.data;
  },

  async update(id, workOrder) {
    await api.put(`/WorkOrdersApi/${id}`, workOrder);
  },

  async delete(id) {
    await api.delete(`/WorkOrdersApi/${id}`);
  },
};

