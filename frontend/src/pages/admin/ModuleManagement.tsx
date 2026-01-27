import { useEffect, useState } from 'react';
import { adminService } from '../../services/adminService';
import type { ModuleResponse } from '../../services/adminService';

export default function ModuleManagement() {
  const [modules, setModules] = useState<ModuleResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [expandedModule, setExpandedModule] = useState<number | null>(null);

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      const response = await adminService.getModules();
      setModules(response.data);
    } catch (err) {
      setError('Moduller yuklenemedi');
    } finally {
      setLoading(false);
    }
  };

  const toggleModule = (moduleId: number) => {
    setExpandedModule(prev => prev === moduleId ? null : moduleId);
  };

  if (loading) return <div className="loading">Yukleniyor...</div>;

  return (
    <div className="admin-page">
      <div className="admin-header">
        <h2>Modul Yonetimi</h2>
      </div>

      {error && <div className="error-message">{error}</div>}

      <div className="modules-tree">
        {modules.map(module => (
          <div key={module.id} className="module-item">
            <div
              className={`module-header ${expandedModule === module.id ? 'expanded' : ''}`}
              onClick={() => toggleModule(module.id)}
            >
              <span className="expand-icon">{expandedModule === module.id ? '▼' : '▶'}</span>
              <span className="module-icon">{module.icon || '📦'}</span>
              <span className="module-name">{module.name}</span>
              <span className="module-code">{module.code}</span>
              <span className={`status ${module.isActive ? 'approved' : 'rejected'}`}>
                {module.isActive ? 'Aktif' : 'Pasif'}
              </span>
            </div>

            {expandedModule === module.id && (
              <div className="module-content">
                {module.description && (
                  <p className="module-description">{module.description}</p>
                )}

                <div className="submodules">
                  <h4>Alt Moduller</h4>
                  {module.subModules.length > 0 ? (
                    module.subModules.map(subModule => (
                      <div key={subModule.id} className="submodule-item">
                        <div className="submodule-header">
                          <strong>{subModule.name}</strong>
                          <code>{subModule.code}</code>
                        </div>
                        {subModule.description && (
                          <p className="submodule-description">{subModule.description}</p>
                        )}

                        <div className="permissions-list">
                          <h5>Yetkiler</h5>
                          {subModule.permissions.length > 0 ? (
                            <table className="permissions-table">
                              <thead>
                                <tr>
                                  <th>Kod</th>
                                  <th>Ad</th>
                                  <th>Seviye</th>
                                </tr>
                              </thead>
                              <tbody>
                                {subModule.permissions.map(perm => (
                                  <tr key={perm.id}>
                                    <td><code>{perm.code}</code></td>
                                    <td>{perm.name}</td>
                                    <td>
                                      <span className="level-badge">{perm.levelName}</span>
                                    </td>
                                  </tr>
                                ))}
                              </tbody>
                            </table>
                          ) : (
                            <p className="no-data">Yetki tanimlanmamis</p>
                          )}
                        </div>
                      </div>
                    ))
                  ) : (
                    <p className="no-data">Alt modul yok</p>
                  )}
                </div>
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
