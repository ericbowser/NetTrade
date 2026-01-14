import React from 'react';

const Header = ({ title }) => {
    return (
        <header className="bg-primary shadow">
            <div className="container mx-auto px-4 py-6">
                <h1 className="text-2xl font-bold text-white">{title}</h1>
            </div>
        </header>
    );
};

export default Header;