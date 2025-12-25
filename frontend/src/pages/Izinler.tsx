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
        // URL'de kullanici ID'si YOK
        const response = await api.get<IzinData>('/ik/izin/me');
        setIzinData(response.data);
      } catch {
        setError('Izinler yuklenemedi');
      } finally {
        setLoading(false);
      }
    };

    fetchIzinler();
  }, []);

  const getDurumClass = (durum: string) => {
    switch (durum.toLowerCase()) {
      case 'onaylandi':
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
        <h1>Izinlerim</h1>
      </header>

      {loading && <div className="loading">Yukleniyor...</div>}
      {error && <div className="error-message">{error}</div>}

      {izinData && !loading && (
        <>
          <div className="izin-summary">
            <div className="summary-card">
              <span className="summary-value">{izinData.yillikIzinHakki}</span>
              <span className="summary-label">Toplam Izin Hakki</span>
            </div>
            <div className="summary-card">
              <span className="summary-value">{izinData.kullanilanIzin}</span>
              <span className="summary-label">Kullanilan</span>
            </div>
            <div className="summary-card highlight">
              <span className="summary-value">{izinData.kalanIzin}</span>
              <span className="summary-label">Kalan Izin</span>
            </div>
          </div>

          <div className="izin-list">
            <h2>Izin Gecmisi</h2>
            <table>
              <thead>
                <tr>
                  <th>Baslangic</th>
                  <th>Bitis</th>
                  <th>Gun</th>
                  <th>Tur</th>
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

          <button className="primary-btn">Yeni Izin Talebi</button>
        </>
      )}
    </div>
  );
};

export default Izinler;
