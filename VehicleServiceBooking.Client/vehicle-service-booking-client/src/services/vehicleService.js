import api from './api';

export const vehicleService = {
  async getAll() {
    const response = await api.get('/VehiclesApi'); // Ndrysho këtu
    return response.data;
  },

  async getById(id) {
    const response = await api.get(`/VehiclesApi/${id}`); // Ndrysho këtu
    return response.data;
  },

  async create(vehicle) {
    const response = await api.post('/VehiclesApi', vehicle); // Ndrysho këtu
    return response.data;
  },

  async update(id, vehicle) {
    await api.put(`/VehiclesApi/${id}`, vehicle); // Ndrysho këtu
  },

  async delete(id) {
    await api.delete(`/VehiclesApi/${id}`); // Ndrysho këtu
  },
};