import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import axios from "axios";
import "../style/style.css";

const Leaderboard = () => {
    const navigate = useNavigate();
    const [leaderboard, setLeaderboard] = useState([]);

    useEffect(() => {
        const fetchLeaderboard = async () => {
            try {
                const response = await axios.get("/api/leaderboard");
                if (response.status === 200) {
                    setLeaderboard(response.data);
                }
            } catch (error) {
                console.error("Error fetching leaderboard:", error);
            }
        };

        fetchLeaderboard();
    }, []);

    const handleBack = () => {
        navigate("/dashboard");
    };

    return (
        <div className="enableer-dashboard">
             <div className="enableer-header">
                <button className="configure-button" onClick={handleBack}>
                    Back to Dashboard
                </button>
            </div>
            <div className="enableer-content">
                <div className="enableer-logo">Leaderboard</div>
                <div className="enableer-input-container">
                    <table className="leaderboard-table" style={{width: '100%', textAlign: 'left', borderCollapse: 'collapse'}}>
                        <thead>
                            <tr style={{borderBottom: '1px solid #ccc'}}>
                                <th style={{padding: '10px'}}>User</th>
                                <th style={{padding: '10px'}}>Points</th>
                            </tr>
                        </thead>
                        <tbody>
                            {leaderboard.map((entry) => (
                                <tr key={entry.userId} style={{borderBottom: '1px solid #eee'}}>
                                    <td style={{padding: '10px'}}>{entry.displayName || "Unknown User"}</td>
                                    <td style={{padding: '10px'}}>{entry.points}</td>
                                </tr>
                            ))}
                            {leaderboard.length === 0 && (
                                <tr>
                                    <td colSpan="2" style={{padding: '20px', textAlign: 'center'}}>No points awarded yet.</td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    );
};

export default Leaderboard;

