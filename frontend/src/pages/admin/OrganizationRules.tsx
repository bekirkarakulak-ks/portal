import { useEffect, useState } from 'react';
import { adminService } from '../../services/adminService';
import type { OrganizationResponse, RoleResponse } from '../../services/adminService';

export default function OrganizationRules() {
  const [organizations, setOrganizations] = useState<OrganizationResponse[]>([]);
  const [roles, setRoles] = useState<RoleResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [editingOrg, setEditingOrg] = useState<OrganizationResponse | null>(null);
  const [formData, setFormData] = useState({
    emailPattern: '',
    departmentCode: '',
    departmentName: '',
    defaultRoleId: 1,
    priority: 0
  });

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      const [orgsRes, rolesRes] = await Promise.all([
        adminService.getOrganizations(),
        adminService.getRoles()
      ]);
      setOrganizations(orgsRes.data);
      setRoles(rolesRes.data);
    } catch (err) {
      setError('Veriler yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = () => {
    setEditingOrg(null);
    setFormData({
      emailPattern: '',
      departmentCode: '',
      departmentName: '',
      defaultRoleId: 1,
      priority: 0
    });
    setShowModal(true);
  };

  const handleEdit = (org: OrganizationResponse) => {
    setEditingOrg(org);
    setFormData({
      emailPattern: org.emailPattern,
      departmentCode: org.departmentCode,
      departmentName: org.departmentName,
      defaultRoleId: org.defaultRoleId,
      priority: org.priority
    });
    setShowModal(true);
  };

  const handleSave = async () => {
    try {
      if (editingOrg) {
        await adminService.updateOrganization(editingOrg.id, {
          ...formData,
          isActive: true
        });
      } else {
        await adminService.createOrganization(formData);
      }
      setShowModal(false);
      fetchData();
    } catch (err) {
      setError('Kaydetme başarısız');
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Bu kuralı silmek istediğinizden emin misiniz?')) return;
    try {
      await adminService.deleteOrganization(id);
      fetchData();
    } catch (err) {
      setError('Silme başarısız');
    }
  };

  if (loading) return <div className="loading">Yükleniyor...</div>;

  return (
    <div className="admin-page">
      <div className="admin-header">
        <h2>Organizasyon Kuralları</h2>
        <button className="btn-primary" onClick={handleCreate}>
          + Yeni Kural
        </button>
      </div>

      <div className="info-box">
        <p>
          <strong>Email Pattern Kullanımı:</strong> Yeni kayıt olan kullanıcıların email adreslerine göre
          otomatik rol atanır. % karakteri joker olarak kullanılır.
        </p>
        <p>Örnek: <code>%@hr.sirket.com</code> → hr.sirket.com ile biten tüm emailler</p>
      </div>

      {error && <div className="error-message">{error}</div>}

      <table className="admin-table">
        <thead>
          <tr>
            <th>Email Pattern</th>
            <th>Departman</th>
            <th>Varsayılan Rol</th>
            <th>Öncelik</th>
            <th>Durum</th>
            <th>İşlemler</th>
          </tr>
        </thead>
        <tbody>
          {organizations.map(org => (
            <tr key={org.id}>
              <td><code>{org.emailPattern}</code></td>
              <td>
                <strong>{org.departmentName}</strong>
                <br />
                <small>{org.departmentCode}</small>
              </td>
              <td>{org.defaultRoleName || '-'}</td>
              <td>{org.priority}</td>
              <td>
                <span className={`status ${org.isActive ? 'approved' : 'rejected'}`}>
                  {org.isActive ? 'Aktif' : 'Pasif'}
                </span>
              </td>
              <td>
                <div className="table-actions">
                  <button className="btn-small btn-primary" onClick={() => handleEdit(org)}>
                    Düzenle
                  </button>
                  <button className="btn-small btn-danger" onClick={() => handleDelete(org.id)}>
                    Sil
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {/* Organization Modal */}
      {showModal && (
        <div className="modal-overlay" onClick={() => setShowModal(false)}>
          <div className="modal" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h3>{editingOrg ? 'Kural Düzenle' : 'Yeni Kural'}</h3>
              <button className="modal-close" onClick={() => setShowModal(false)}>×</button>
            </div>
            <div className="modal-body">
              <div className="form-group">
                <label>Email Pattern</label>
                <input
                  type="text"
                  value={formData.emailPattern}
                  onChange={e => setFormData(prev => ({ ...prev, emailPattern: e.target.value }))}
                  placeholder="%@sirket.com"
                />
                <small>% karakteri joker olarak kullanılır</small>
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label>Departman Kodu</label>
                  <input
                    type="text"
                    value={formData.departmentCode}
                    onChange={e => setFormData(prev => ({ ...prev, departmentCode: e.target.value }))}
                    placeholder="IK"
                  />
                </div>
                <div className="form-group">
                  <label>Departman Adı</label>
                  <input
                    type="text"
                    value={formData.departmentName}
                    onChange={e => setFormData(prev => ({ ...prev, departmentName: e.target.value }))}
                    placeholder="İnsan Kaynakları"
                  />
                </div>
              </div>
              <div className="form-group">
                <label>Varsayılan Rol</label>
                <select
                  value={formData.defaultRoleId}
                  onChange={e => setFormData(prev => ({ ...prev, defaultRoleId: Number(e.target.value) }))}
                >
                  {roles.map(role => (
                    <option key={role.id} value={role.id}>{role.name}</option>
                  ))}
                </select>
              </div>
              <div className="form-group">
                <label>Öncelik (yüksek değer = öncelikli)</label>
                <input
                  type="number"
                  value={formData.priority}
                  onChange={e => setFormData(prev => ({ ...prev, priority: Number(e.target.value) }))}
                />
                <small>Birden fazla kural eşleştiyse, en yüksek öncelikli kural uygulanır</small>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn-secondary" onClick={() => setShowModal(false)}>
                İptal
              </button>
              <button className="btn-primary" onClick={handleSave}>
                Kaydet
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
