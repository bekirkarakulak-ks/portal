import { Link } from 'react-router-dom';

export const Unauthorized = () => {
  return (
    <div className="error-page">
      <div className="error-content">
        <h1>403</h1>
        <h2>Erişim Engellendi</h2>
        <p>Bu sayfaya erişim yetkiniz bulunmamaktadır.</p>
        <Link to="/" className="primary-btn">
          Ana Sayfaya Dön
        </Link>
      </div>
    </div>
  );
};

export default Unauthorized;
