import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import api from '../services/api';

interface BordroData {
  userId: number;
  username: string;
  ay: number;
  yil: number;
  brutMaas: number;
  netMaas: number;
  sgkIsci: number;
  gelirVergisi: number;
  kesintiler: { ad: string; tutar: number }[];
}

export const Bordro = () => {
  const [bordro, setBordro] = useState<BordroData | null>(null);
  const [ay, setAy] = useState(new Date().getMonth() + 1);
  const [yil, setYil] = useState(new Date().getFullYear());
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchBordro = async () => {
      setLoading(true);
      setError('');
      try {
        // URL'de kullanıcı ID'si YOK - backend session'dan alıyor
        const response = await api.get<BordroData>('/ik/bordro/me', {
          params: { ay, yil },
        });
        setBordro(response.data);
      } catch {
        setError('Bordro yüklenemedi');
      } finally {
        setLoading(false);
      }
    };

    fetchBordro();
  }, [ay, yil]);

  const aylar = [
    'Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran',
    'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık',
  ];

  return (
    <div className="page-container">
      <header className="page-header">
        <Link to="/" className="back-link">← Ana Sayfa</Link>
        <h1>Bordrom</h1>
      </header>

      <div className="filters">
        <select value={ay} onChange={(e) => setAy(Number(e.target.value))}>
          {aylar.map((ayAdi, index) => (
            <option key={index} value={index + 1}>
              {ayAdi}
            </option>
          ))}
        </select>
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

      {bordro && !loading && (
        <div className="bordro-card">
          <h2>
            {aylar[bordro.ay - 1]} {bordro.yil} Bordrosu
          </h2>
          <div className="bordro-details">
            <div className="bordro-row">
              <span>Brüt Maaş:</span>
              <span className="amount">{bordro.brutMaas.toLocaleString('tr-TR')} TL</span>
            </div>
            <div className="bordro-row kesinti-header">
              <span>Kesintiler:</span>
            </div>
            {bordro.kesintiler.map((kesinti, index) => (
              <div key={index} className="bordro-row kesinti">
                <span>- {kesinti.ad}:</span>
                <span className="amount negative">-{kesinti.tutar.toLocaleString('tr-TR')} TL</span>
              </div>
            ))}
            <div className="bordro-row total">
              <span>Net Maaş:</span>
              <span className="amount">{bordro.netMaas.toLocaleString('tr-TR')} TL</span>
            </div>
          </div>
        </div>
      )}

      <div className="security-note">
        <p>
          <strong>Güvenlik Notu:</strong> Bu sayfa sadece sizin bordronuzu gösterir.
          Link paylaşıldığında bile, herkes kendi bordrosunu görebilir.
        </p>
      </div>
    </div>
  );
};

export default Bordro;
