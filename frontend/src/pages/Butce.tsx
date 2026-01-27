import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import api from '../services/api';

interface ButceData {
  userId: number;
  username: string;
  yil: number;
  toplamButce: number;
  kullanilanButce: number;
  kalanButce: number;
  harcamalar: {
    id: number;
    kategori: string;
    tutar: number;
    tarih: string;
    aciklama: string;
  }[];
}

export const Butce = () => {
  const [butce, setButce] = useState<ButceData | null>(null);
  const [yil, setYil] = useState(new Date().getFullYear());
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchButce = async () => {
      setLoading(true);
      setError('');
      try {
        // URL'de kullanıcı ID'si YOK
        const response = await api.get<ButceData>('/butce/me', {
          params: { yil },
        });
        setButce(response.data);
      } catch {
        setError('Bütçe bilgisi yüklenemedi');
      } finally {
        setLoading(false);
      }
    };

    fetchButce();
  }, [yil]);

  const getKullanilmaOrani = () => {
    if (!butce) return 0;
    return Math.round((butce.kullanilanButce / butce.toplamButce) * 100);
  };

  return (
    <div className="page-container">
      <header className="page-header">
        <Link to="/" className="back-link">← Ana Sayfa</Link>
        <h1>Bütçem</h1>
      </header>

      <div className="filters">
        <select value={yil} onChange={(e) => setYil(Number(e.target.value))}>
          {[2023, 2024, 2025].map((y) => (
            <option key={y} value={y}>
              {y}
            </option>
          ))}
        </select>
      </div>

      {loading && <div className="loading">Yükleniyor...</div>}
      {error && <div className="error-message">{error}</div>}

      {butce && !loading && (
        <>
          <div className="butce-summary">
            <div className="butce-overview">
              <h2>{yil} Yılı Bütçesi</h2>
              <div className="butce-progress">
                <div className="progress-bar">
                  <div
                    className="progress-fill"
                    style={{ width: `${getKullanilmaOrani()}%` }}
                  />
                </div>
                <span className="progress-text">%{getKullanilmaOrani()} kullanıldı</span>
              </div>
            </div>

            <div className="butce-stats">
              <div className="stat-card">
                <span className="stat-value">
                  {butce.toplamButce.toLocaleString('tr-TR')} TL
                </span>
                <span className="stat-label">Toplam Bütçe</span>
              </div>
              <div className="stat-card used">
                <span className="stat-value">
                  {butce.kullanilanButce.toLocaleString('tr-TR')} TL
                </span>
                <span className="stat-label">Kullanılan</span>
              </div>
              <div className="stat-card remaining">
                <span className="stat-value">
                  {butce.kalanButce.toLocaleString('tr-TR')} TL
                </span>
                <span className="stat-label">Kalan</span>
              </div>
            </div>
          </div>

          <div className="harcama-list">
            <h2>Harcamalarım</h2>
            <table>
              <thead>
                <tr>
                  <th>Tarih</th>
                  <th>Kategori</th>
                  <th>Açıklama</th>
                  <th>Tutar</th>
                </tr>
              </thead>
              <tbody>
                {butce.harcamalar.map((harcama) => (
                  <tr key={harcama.id}>
                    <td>{harcama.tarih}</td>
                    <td>{harcama.kategori}</td>
                    <td>{harcama.aciklama}</td>
                    <td className="amount">
                      {harcama.tutar.toLocaleString('tr-TR')} TL
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <button className="primary-btn">Yeni Harcama Ekle</button>
        </>
      )}
    </div>
  );
};

export default Butce;
