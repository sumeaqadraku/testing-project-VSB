import React, { useState, useEffect } from 'react';
import Layout from '../components/Layout';
import { serviceCenterService } from '../services/serviceCenterService';
import { serviceTypeService } from '../services/serviceTypeService';
import { mechanicService } from '../services/mechanicService';
import { partService } from '../services/partService';
import { scheduleService } from '../services/scheduleService';
import { bookingService } from '../services/bookingService';
import { workOrderService } from '../services/workOrderService';
import { paymentService } from '../services/paymentService';
import { invoiceService } from '../services/invoiceService';

const ManagerDashboard = () => {
  const [activeTab, setActiveTab] = useState('servicecenters');
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(false);
  const [showModal, setShowModal] = useState(false);
  const [editingItem, setEditingItem] = useState(null);
  const [showInvoiceModal, setShowInvoiceModal] = useState(false);
  const [selectedWorkOrder, setSelectedWorkOrder] = useState(null);

  const tabs = [
    { id: 'servicecenters', label: 'Service Centers' },
    { id: 'servicetypes', label: 'Service Types' },
    { id: 'mechanics', label: 'Mechanics' },
    { id: 'parts', label: 'Parts' },
    { id: 'schedules', label: 'Schedules' },
    { id: 'bookings', label: 'Bookings' },
    { id: 'workorders', label: 'Work Orders' },
    { id: 'payments', label: 'Payments' },
  ];

  const getService = () => ({
    servicecenters: serviceCenterService,
    servicetypes: serviceTypeService,
    mechanics: mechanicService,
    parts: partService,
    schedules: scheduleService,
    bookings: bookingService,
    workorders: workOrderService,
    payments: paymentService
  }[activeTab]);

  useEffect(() => { loadData(); }, [activeTab]);

  const loadData = async () => {
    setLoading(true);
    try {
      const data = await getService().getAll();
      setItems(Array.isArray(data) ? data : []);
    } catch (err) { console.error(err); } 
    finally { setLoading(false); }
  };

  const handleSave = async (formData) => {
    try {
      const id = editingItem ? editingItem.id : null;
      let { mechanic, client, vehicle, booking, ...payload } = formData;
      if (id) {
        payload.id = id;
      }

    if (activeTab === 'mechanics') {
      payload.serviceCenterId = parseInt(payload.serviceCenterId);
      payload.hourlyRate = parseFloat(payload.hourlyRate);
      payload.isAvailable = formData.isAvailable === undefined ? true : formData.isAvailable;
    }
      if (activeTab === 'schedules') {
      payload.dayOfWeek = parseInt(payload.dayOfWeek);
      if (payload.startTime?.length === 5) payload.startTime += ":00";
      if (payload.endTime?.length === 5) payload.endTime += ":00";
    }
      if (activeTab === 'bookings') {
    payload.serviceCenterId = parseInt(payload.serviceCenterId);
    payload.vehicleId = parseInt(payload.vehicleId);
    payload.serviceTypeId = parseInt(payload.serviceTypeId);
    if (payload.bookingTime?.length === 5) payload.bookingTime += ":00"; 
      }

      if (activeTab === 'workorders') {
        payload.status = parseInt(payload.status);
        if (payload.laborCost) payload.laborCost = parseFloat(payload.laborCost);
        if (payload.partsCost) payload.partsCost = parseFloat(payload.partsCost);
        if (payload.totalCost) payload.totalCost = parseFloat(payload.totalCost);
        if (payload.estimatedDurationMinutes) payload.estimatedDurationMinutes = parseInt(payload.estimatedDurationMinutes);
        if (payload.actualDurationMinutes) payload.actualDurationMinutes = parseInt(payload.actualDurationMinutes);
      }

      if (activeTab === 'vehicles') {
          
          payload.licensePlate = payload.licensePlate?.toUpperCase(); 
      }

      if (editingItem) {
        await getService().update(editingItem.id, payload);
      } else {
        await getService().create(payload);
      }
      setShowModal(false);
      loadData();
    } catch (error) {
      alert("Gabim gjatë ruajtjes. Kontrolloni konsollën.");
    }
  };

  return (
    <Layout>
      <div className="p-8 max-w-6xl mx-auto">
        <div className="flex justify-between items-center mb-8">
          <h1 className="text-3xl font-bold text-gray-900">Manager Dashboard</h1>
          {!['bookings', 'workorders', 'payments'].includes(activeTab) && (
            <button onClick={() => { setEditingItem(null); setShowModal(true); }} className="bg-emerald-600 text-white px-6 py-2.5 rounded-xl font-bold shadow-lg hover:bg-emerald-700 transition-all">+ New {activeTab}</button>
          )}
        </div>

        <div className="flex space-x-2 mb-8 bg-white p-2 rounded-2xl shadow-sm border border-gray-100 overflow-x-auto">
          {tabs.map(tab => (
            <button key={tab.id} onClick={() => setActiveTab(tab.id)} className={`px-4 py-2 rounded-lg font-bold transition-all ${activeTab === tab.id ? 'bg-emerald-600 text-white shadow-md' : 'text-gray-500 hover:bg-gray-50'}`}>{tab.label}</button>
          ))}
        </div>

        <div className="bg-white rounded-3xl shadow-sm border border-gray-100 overflow-hidden">
          {loading ? <div className="p-20 text-center text-gray-400">Loading...</div> : (
            <table className="w-full text-left">
              <thead className="bg-gray-50 border-b border-gray-100">
                <tr>
                  <th className="px-8 py-5 text-xs font-bold text-gray-400 uppercase tracking-widest">Details</th>
                  <th className="px-8 py-5 text-right text-xs font-bold text-gray-400 uppercase tracking-widest">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {items.map(item => (
                  <tr key={item.id} className="hover:bg-emerald-50/30 transition-colors">
                   <td className="px-8 py-5 font-bold text-gray-800">
                      <div className="flex flex-col">
                        <span>
                        {activeTab === 'bookings' 
                          ? (item.mechanic ? `Mekaniku: ${item.mechanic.firstName} ${item.mechanic.lastName}` : "Mekaniku: I pacaktuar")
                          : activeTab === 'mechanics' 
                            ? `${item.firstName} ${item.lastName}` 
                            : activeTab === 'schedules'
                              ? (item.fullName || `ID: ${item.id}`)
                              : (item.name || `ID: ${item.id}`)}
                      </span>
                        {activeTab === 'mechanics' && (
                          <div className="flex flex-wrap gap-2 mt-1.5 font-normal">
                            <span className="text-[10px] bg-purple-50 text-purple-700 px-2 py-0.5 rounded-md border border-purple-100">
                              {item.specialization}
                            </span>
                            <span className="text-[10px] bg-amber-50 text-amber-700 px-2 py-0.5 rounded-md border border-amber-100">
                              ${item.hourlyRate}/hr
                            </span>
                          </div>
                        )}
                        {activeTab === 'schedules' && (
                          <div className="flex gap-2 mt-1.5 font-normal">
                            <span className="text-[10px] bg-blue-50 text-blue-700 px-2 py-0.5 rounded-md border border-blue-100">
                              {item.startTime} - {item.endTime}
                            </span>
                          </div>
                        )}
                        {activeTab === 'servicetypes' && (
                          <div className="flex gap-2 mt-1.5 font-normal">
                            <span className="text-[10px] bg-emerald-50 text-emerald-700 px-2 py-0.5 rounded-md border border-emerald-100">
                              Price: ${item.basePrice}
                            </span>
                            <span className="text-[10px] bg-blue-50 text-blue-700 px-2 py-0.5 rounded-md border border-blue-100">
                              Time: {item.estimatedDurationMinutes} min
                            </span>
                          </div>
                          
                        )}
                        {activeTab === 'servicecenters' && (
                          <div className="flex flex-col gap-1 mt-1.5 font-normal">
                            <span className="text-[11px] text-gray-500 italic">
                              {item.address}
                            </span>
                            <span className="text-[10px] bg-gray-100 text-gray-600 px-2 py-0.5 rounded-md border border-gray-200 w-fit font-semibold uppercase">
                              City: {item.city}
                            </span>
                          </div>
                        )}
                        {activeTab === 'parts' && item.stockQuantity !== undefined && (
                          <div className="mt-1.5 font-normal">
                            <span className="text-[10px] bg-emerald-50 text-emerald-700 px-2 py-0.5 rounded-md border border-emerald-100 font-semibold uppercase tracking-wider">
                              Stock: {item.stockQuantity} items
                            </span>
                          </div>
                        )}
                 {activeTab === 'bookings' && (
                    <div className="flex flex-col gap-1 mt-1 font-normal">
                      
                      <span className="text-sm font-bold text-emerald-700">
                        Klienti: {item.client?.firstName} {item.client?.lastName}
                      </span>
                      
                     
                      <span className="text-[11px] text-gray-600 font-semibold bg-gray-100 px-2 py-0.5 rounded w-fit">
                        Targa: {item.vehicle?.licensePlate || 'Pa targë'}
                      </span>

                      
                      <span className="text-[11px] text-gray-500">
                        Data: {new Date(item.bookingDate).toLocaleDateString()} në {item.bookingTime}
                      </span>
                    </div>
                  )}
                  {activeTab === 'workorders' && (
                    <div className="flex flex-col gap-1 mt-1.5 font-normal">
                      <span className="text-[10px] bg-orange-50 text-orange-700 px-2 py-0.5 rounded-md border border-orange-100 w-fit font-semibold uppercase">
                        Status: {item.status === 0 ? 'Scheduled' : item.status === 1 ? 'In Progress' : item.status === 2 ? 'Completed' : item.status === 3 ? 'Ready For Payment' : 'Closed'}
                      </span>
                      {item.booking && (
                        <>
                          <span className="text-sm font-bold text-emerald-700">
                            Klienti: {item.booking.client?.firstName} {item.booking.client?.lastName}
                          </span>
                          {item.booking.vehicle && (
                            <span className="text-[11px] text-gray-600 font-semibold bg-gray-100 px-2 py-0.5 rounded w-fit">
                              Makina: {item.booking.vehicle.make} {item.booking.vehicle.model} - {item.booking.vehicle.licensePlate}
                            </span>
                          )}
                        </>
                      )}
                      {item.mechanic && (
                        <span className="text-[11px] text-gray-500">
                          Mekaniku: {item.mechanic.firstName} {item.mechanic.lastName} - {item.mechanic.specialization}
                        </span>
                      )}
                      {item.totalCost && (
                        <span className="text-[11px] bg-blue-50 text-blue-700 px-2 py-0.5 rounded-md border border-blue-100 w-fit font-semibold">
                          Total: ${parseFloat(item.totalCost).toFixed(2)}
                        </span>
                      )}
                    </div>
                  )}
                 {activeTab === 'payments' && (
                  <div className="flex flex-col gap-1 mt-1.5 font-normal">  
                    <span className="text-sm font-bold text-gray-800">
                      Klienti: {
                        item.workOrder?.booking?.client 
                          ? `${item.workOrder.booking.client.firstName} ${item.workOrder.booking.client.lastName}` 
                          : "I panjohur"
                      }
                    </span>

                    <span className="text-[10px] bg-emerald-50 text-emerald-700 px-2 py-0.5 rounded-md border border-emerald-100 w-fit font-semibold uppercase">
                      Shuma: ${item.amount?.toFixed(2)}
                    </span>

                    <span className="text-[11px] text-gray-600">
                      Metoda: <span className="text-blue-600 font-bold">
                        {{
                          0: 'Cash',
                          1: 'Credit Card',
                          2: 'Debit Card',
                          3: 'Bank Transfer',
                          4: 'Online'
                        }[item.method] || 'E papërcaktuar'}
                      </span>
                    </span>

                    {item.transactionId && (
                      <span className="text-[10px] text-gray-400 italic">
                        Nr. Transaksionit: {item.transactionId}
                      </span>
                    )}
                  </div>
                )}
                      </div>
                    </td>
                    <td className="px-8 py-5 text-right">
                      <button
                        onClick={() => { setEditingItem(item); setShowModal(true); }}
                        className="text-emerald-600 font-bold mr-4 hover:underline"
                      >
                        Edit
                      </button>
                      {activeTab === 'workorders' && (item.status === 2 || item.status === 3) && (
                        <button
                          onClick={async () => {
                            try {
                              
                              const existingInvoice = await invoiceService.getByWorkOrder(item.id).catch(() => null);
                              if (existingInvoice) {
                                alert('Invoice already exists for this WorkOrder');
                                return;
                              }
                              setSelectedWorkOrder(item);
                              setShowInvoiceModal(true);
                            } catch (error) {
                              console.error('Error checking invoice:', error);
                            }
                          }}
                          className="text-blue-600 font-bold mr-4 hover:underline"
                        >
                          Create Invoice
                        </button>
                      )}
                      {activeTab !== 'bookings' && (
                        <button
                          onClick={() => getService().delete(item.id).then(loadData)}
                          className="text-red-500 font-bold hover:underline"
                        >
                          Delete
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
        {showModal && <ManagerModal type={activeTab} item={editingItem} onClose={() => setShowModal(false)} onSave={handleSave} />}
        {showInvoiceModal && selectedWorkOrder && (
          <InvoiceModal
            workOrder={selectedWorkOrder}
            onClose={() => { setShowInvoiceModal(false); setSelectedWorkOrder(null); }}
            onSave={async (invoiceData) => {
              try {
                await invoiceService.create(invoiceData);
                alert('Invoice created successfully!');
                setShowInvoiceModal(false);
                setSelectedWorkOrder(null);
                loadData();
              } catch (error) {
                alert(error.response?.data?.message || 'Error creating invoice');
              }
            }}
          />
        )}
      </div>
    </Layout>
  );
};

const ManagerModal = ({ type, item, onClose, onSave }) => {
  const [formData, setFormData] = useState(item || {});
  const [mechanics, setMechanics] = useState([]); 

 
useEffect(() => {
  if (type === 'bookings' || type === 'schedules') {
    mechanicService.getAll().then(data => {
      const mechanicsList = Array.isArray(data) ? data : [];
      setMechanics(mechanicsList);
      if (item && item.fullName && !formData.firstName) {
        const names = item.fullName.split(" ");
        setFormData(prev => ({
          ...prev,
          firstName: names[0],
          lastName: names[1] || ""
        }));
      }
    });
  }
}, [type, item]);


  const handleChange = (e) => setFormData({ ...formData, [e.target.name]: e.target.value });

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-3xl w-full max-w-md overflow-hidden shadow-2xl">
        <div className="px-8 py-6 border-b flex justify-between bg-gray-50/50">
          <h3 className="text-xl font-bold capitalize">{item ? 'Edit' : 'Create'} {type}</h3>
          <button onClick={onClose} className="text-2xl text-gray-400">&times;</button>
        </div>
        <form onSubmit={(e) => { e.preventDefault(); onSave(formData); }} className="p-8 space-y-4 max-h-[80vh] overflow-y-auto">
          
          {type === 'servicecenters' && (
            <>
              <Input label="Name" name="name" value={formData.name} onChange={handleChange} required />
              <Input label="Address" name="address" value={formData.address} onChange={handleChange} required />
              <Input label="City" name="city" value={formData.city} onChange={handleChange} required />
            </>
          )}

          {type === 'mechanics' && (
  <>
            <div className="grid grid-cols-2 gap-4">
              <Input label="First Name" name="firstName" value={formData.firstName} onChange={handleChange} required />
              <Input label="Last Name" name="lastName" value={formData.lastName} onChange={handleChange} required />
            </div>
            <Input label="Email" name="email" type="email" value={formData.email} onChange={handleChange} required />
            {!item && <Input label="Password" name="password" type="password" value={formData.password} onChange={handleChange} required />}
            <Input label="Specialization" name="specialization" value={formData.specialization} onChange={handleChange} required />
            <div className="grid grid-cols-2 gap-4">
              <Input label="Center ID" name="serviceCenterId" type="number" min="0" value={formData.serviceCenterId} onChange={handleChange} required />
              <Input label="Rate ($)" name="hourlyRate" type="number" min="0" value={formData.hourlyRate} onChange={handleChange} required />
            </div>

            <div className="w-full">
              <label className="block text-xs font-bold text-gray-400 uppercase tracking-widest mb-1.5 ml-1">Availability</label>
              <select 
                name="isAvailable" 
                value={formData.isAvailable !== undefined ? formData.isAvailable : true} 
                onChange={(e) => setFormData({...formData, isAvailable: e.target.value === 'true'})}
                className="w-full bg-gray-50 border-gray-100 border-2 px-4 py-2.5 rounded-xl focus:border-emerald-500 outline-none transition-all text-gray-700 font-bold"
              >
                <option value="true">Available (Yes)</option>
                <option value="false">Not Available (No)</option>
              </select>
            </div>
          </>
        )}

          {type === 'schedules' && (
  <>
              {!item && (
                <div className="w-full">
                  <label className="block text-xs font-bold text-gray-400 uppercase tracking-widest mb-1.5 ml-1">
                    Zgjidh Mekanikun
                  </label>
                  <select 
                    name="mechanicId" 
                    value={formData.mechanicId || ""} 
                    className="w-full bg-gray-50 border-gray-100 border-2 px-4 py-2.5 rounded-xl focus:border-emerald-500 outline-none transition-all text-gray-700 font-bold"
                    onChange={(e) => {
                      const selectedId = parseInt(e.target.value);
                      const selected = mechanics.find(m => m.id === selectedId);
                      if (selected) {
                        setFormData({ 
                          ...formData, 
                          mechanicId: selected.id,
                          firstName: selected.firstName || selected.user?.firstName, 
                          lastName: selected.lastName || selected.user?.lastName 
                        });
                      }
                    }}
                    required
                  >
                    <option value="">Zgjidh...</option>
                    {mechanics.map(m => (
                      <option key={m.id} value={m.id}>{m.firstName} {m.lastName}</option>
                    ))}
                  </select>
                </div>
              )}

              
              {item && (
                <div className="w-full bg-gray-50 p-3 rounded-xl border border-gray-100 mb-2">
                  <label className="block text-[10px] font-bold text-gray-400 uppercase tracking-widest mb-1">
                    Mekaniku 
                  </label>
                  <div className="text-gray-700 font-bold">
                    {item.fullName || (item.firstName + " " + item.lastName)}
                  </div>
                </div>
              )}

             <Input label="Dita e Javës" name="dayOfWeek" type="number" min="0" max="6" value={formData.dayOfWeek} onChange={handleChange} placeholder="psh 0=Dielë, 1=Hënë..." autoComplete="off" required />
              <Input label="Start Time" name="startTime" type="time" value={formData.startTime} onChange={handleChange} required />
              <Input label="End Time" name="endTime" type="time" value={formData.endTime} onChange={handleChange} required />
            </>
          )}
          {type === 'servicetypes' && (
            <>
              <Input label="Name" name="name" value={formData.name} onChange={handleChange} required />
              <Input label="Base Price" name="basePrice" type="number" min="0" value={formData.basePrice} onChange={handleChange} required />
              <Input label="Duration" name="estimatedDurationMinutes" type="number" min="0" value={formData.estimatedDurationMinutes} onChange={handleChange} required />
            </>
          )}

          {type === 'parts' && (
            <>
              <Input label="Name" name="name" value={formData.name} onChange={handleChange} required />
              <Input label="Stock" name="stockQuantity" type="number" min="0" value={formData.stockQuantity} onChange={handleChange} required />
            </>
          )}
         {type === 'bookings' && (
            <>
              <div className="w-full">
                <label className="block text-xs font-bold text-gray-400 uppercase tracking-widest mb-1.5 ml-1">Cakto Mekanikun</label>
                <select 
                  name="mechanicId" 
                  value={formData.mechanicId || ''} 
                  onChange={(e) => setFormData({...formData, mechanicId: e.target.value ? parseInt(e.target.value) : null})}
                  className="w-full bg-gray-50 border-gray-100 border-2 px-4 py-2.5 rounded-xl focus:border-emerald-500 outline-none transition-all text-gray-700 font-bold"
                >
                  <option value="">Zgjidh Mekanikun...</option>
                  {mechanics.map(m => (
                    <option key={m.id} value={m.id}>
                      {m.firstName} {m.lastName} - {m.specialization}
                    </option>
                  ))}
                </select>
              </div>
            </>
          )}

          {type === 'workorders' && (
            <>
              <div className="w-full">
                <label className="block text-xs font-bold text-gray-400 uppercase tracking-widest mb-1.5 ml-1">Status</label>
                <select 
                  name="status" 
                  value={formData.status || 0} 
                  onChange={handleChange}
                  className="w-full bg-gray-50 border-gray-100 border-2 px-4 py-2.5 rounded-xl focus:border-emerald-500 outline-none transition-all text-gray-700 font-bold"
                >
                  <option value="0">Scheduled</option>
                  <option value="1">In Progress</option>
                  <option value="2">Completed</option>
                  <option value="3">Ready For Payment</option>
                  <option value="4">Closed</option>
                </select>
              </div>
              <Input label="Description" name="description" value={formData.description} onChange={handleChange} />
              <Input label="Mechanic Notes" name="mechanicNotes" value={formData.mechanicNotes} onChange={handleChange} type="textarea" />
              <div className="grid grid-cols-2 gap-4">
                <Input label="Estimated Duration (min)" name="estimatedDurationMinutes" type="number" value={formData.estimatedDurationMinutes} onChange={handleChange} />
                <Input label="Actual Duration (min)" name="actualDurationMinutes" type="number" value={formData.actualDurationMinutes} onChange={handleChange} />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <Input label="Labor Cost ($)" name="laborCost" type="number" step="0.01" value={formData.laborCost} onChange={handleChange} />
                <Input label="Parts Cost ($)" name="partsCost" type="number" step="0.01" value={formData.partsCost} onChange={handleChange} />
              </div>
              <Input label="Total Cost ($)" name="totalCost" type="number" step="0.01" value={formData.totalCost} onChange={handleChange} />
            </>
          )}

          <div className="flex justify-end gap-3 pt-6">
            <button type="button" onClick={onClose} className="px-4 py-2 text-gray-400 font-bold">Cancel</button>
            <button type="submit" className="bg-emerald-600 text-white px-8 py-2 rounded-xl font-bold">Save Changes</button>
          </div>
        </form>
      </div>
    </div>
  );
};

const InvoiceModal = ({ workOrder, onClose, onSave }) => {
  const [taxRate, setTaxRate] = useState(0.18);

  const handleSubmit = (e) => {
    e.preventDefault();
    onSave({
      workOrderId: workOrder.id,
      taxRate: taxRate
    });
  };

  const subTotal = workOrder.totalCost || 0;
  const taxAmount = subTotal * taxRate;
  const totalAmount = subTotal + taxAmount;

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-3xl w-full max-w-md overflow-hidden shadow-2xl">
        <div className="px-8 py-6 border-b flex justify-between bg-gray-50/50">
          <h3 className="text-xl font-bold">Create Invoice</h3>
          <button onClick={onClose} className="text-2xl text-gray-400">&times;</button>
        </div>
        <form onSubmit={handleSubmit} className="p-8 space-y-4">
          <div className="space-y-2">
            <p className="text-sm text-gray-600">Work Order ID: {workOrder.id}</p>
            {workOrder.booking && (
              <p className="text-sm text-gray-600">
                Client: {workOrder.booking.client?.firstName} {workOrder.booking.client?.lastName}
              </p>
            )}
            <div className="mt-4">
              <label className="block text-xs font-bold text-gray-400 uppercase tracking-widest mb-1.5 ml-1">Tax Rate (%)</label>
              <input
                type="number"
                step="0.01"
                min="0"
                max="1"
                value={taxRate}
                onChange={(e) => setTaxRate(parseFloat(e.target.value) || 0)}
                className="w-full bg-gray-50 border-gray-100 border-2 px-4 py-2.5 rounded-xl focus:border-emerald-500 outline-none transition-all text-gray-700"
                required
              />
            </div>
            <div className="mt-4 space-y-2 pt-4 border-t">
              <div className="flex justify-between">
                <span className="text-gray-600">Subtotal:</span>
                <span className="font-bold">${subTotal.toFixed(2)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-600">Tax ({((taxRate) * 100).toFixed(0)}%):</span>
                <span className="font-bold">${taxAmount.toFixed(2)}</span>
              </div>
              <div className="flex justify-between pt-2 border-t">
                <span className="text-lg font-bold">Total:</span>
                <span className="text-lg font-bold text-emerald-600">${totalAmount.toFixed(2)}</span>
              </div>
            </div>
          </div>
          <div className="flex justify-end gap-3 pt-6">
            <button type="button" onClick={onClose} className="px-4 py-2 text-gray-400 font-bold">Cancel</button>
            <button type="submit" className="bg-emerald-600 text-white px-8 py-2 rounded-xl font-bold">Create Invoice</button>
          </div>
        </form>
      </div>
    </div>
  );
};

const Input = ({ label, value, type, ...props }) => (
  <div className="w-full">
    <label className="block text-xs font-bold text-gray-400 uppercase tracking-widest mb-1.5 ml-1">{label}</label>
    {type === 'textarea' ? (
      <textarea {...props} value={value || ''} className="w-full bg-gray-50 border-gray-100 border-2 px-4 py-2.5 rounded-xl focus:border-emerald-500 outline-none transition-all text-gray-700" rows={4} />
    ) : (
      <input {...props} type={type || 'text'} value={value || ''} className="w-full bg-gray-50 border-gray-100 border-2 px-4 py-2.5 rounded-xl focus:border-emerald-500 outline-none transition-all text-gray-700" />
    )}
  </div>
);

export default ManagerDashboard;