import { Link } from 'react-router-dom';

export const Unauthorized = () => {
  return (
    <div className="error-page">
      <div className="error-content">
        <h1>403</h1>
        <h2>Erisim Engellendi</h2>
        <p>Bu sayfaya erisim yetkiniz bulunmamaktadir.</p>
        <Link to="/" className="primary-btn">
          Ana Sayfaya Don
        </Link>
      </div>
    </div>
  );
};

export default Unauthorized;
