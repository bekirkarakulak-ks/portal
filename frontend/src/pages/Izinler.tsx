import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import api from '../services/api';

interface IzinData {
  userId: number;
  yillikIzinHakki: number;
  kullanilanIzin: number;
  kalanIzin: number;
  izinListesi: {
    id: number;
    baslangicTarihi: string;
    bitisTarihi: string;
    gun: number;
    durum: string;
    tur: string;
  }[];
}

export const Izinler = () => {
  const [izinData, setIzinData] = useState<IzinData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchIzinler = async () => {
      setLoading(true);
      setError('');
      try {
        // URL'de kullanıcı ID'si YOK
        const response = await api.get<IzinData>('/ik/izin/me');
        setIzinData(response.data);
      } catch {
        setError('İzinler yüklenemedi');
      } finally {
        setLoading(false);
      }
    };

    fetchIzinler();
  }, []);

  const getDurumClass = (durum: string) => {
    switch (durum.toLowerCase()) {
      case 'onaylandı':
        return 'status-approved';
      case 'bekliyor':
        return 'status-pending';
      case 'reddedildi':
        return 'status-rejected';
      default:
        return '';
    }
  };

  return (
    <div className="page-container">
      <header className="page-header">
        <Link to="/" className="back-link">← Ana Sayfa</Link>
        <h1>İzinlerim</h1>
      </header>

      {loading && <div className="loading">Yükleniyor...</div>}
      {error && <div className="error-message">{error}</div>}

      {izinData && !loading && (
        <>
          <div className="izin-summary">
            <div className="summary-card">
              <span className="summary-value">{izinData.yillikIzinHakki}</span>
              <span className="summary-label">Toplam İzin Hakkı</span>
            </div>
            <div className="summary-card">
              <span className="summary-value">{izinData.kullanilanIzin}</span>
              <span className="summary-label">Kullanılan</span>
            </div>
            <div className="summary-card highlight">
              <span className="summary-value">{izinData.kalanIzin}</span>
              <span className="summary-label">Kalan İzin</span>
            </div>
          </div>

          <div className="izin-list">
            <h2>İzin Geçmişi</h2>
            <table>
              <thead>
                <tr>
                  <th>Başlangıç</th>
                  <th>Bitiş</th>
                  <th>Gün</th>
                  <th>Tür</th>
                  <th>Durum</th>
                </tr>
              </thead>
              <tbody>
                {izinData.izinListesi.map((izin) => (
                  <tr key={izin.id}>
                    <td>{izin.baslangicTarihi}</td>
                    <td>{izin.bitisTarihi}</td>
                    <td>{izin.gun}</td>
                    <td>{izin.tur}</td>
                    <td>
                      <span className={`status ${getDurumClass(izin.durum)}`}>
                        {izin.durum}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <button className="primary-btn">Yeni İzin Talebi</button>
        </>
      )}
    </div>
  );
};

export default Izinler;
