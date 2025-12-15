// <copyright file="dashboard.jsx" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
// </copyright>

import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import * as microsoftTeams from "@microsoft/teams-js";
import axios from "axios";
import "../style/style.css";

const Dashboard = () => {
    const navigate = useNavigate();
    const [question, setQuestion] = useState("");
    const [selectedTag, setSelectedTag] = useState("");
    const [onlyOnline, setOnlyOnline] = useState(false);
    const [tags, setTags] = useState([]);
    
    useEffect(() => {
        const initTeams = async () => {
            try {
                await microsoftTeams.app.initialize();
                microsoftTeams.app.notifySuccess();
                
                const context = await microsoftTeams.app.getContext();
                const token = await microsoftTeams.authentication.getAuthToken();
                const teamId = context.team.groupId;
                
                if (teamId && token) {
                    const response = await axios.get(`/api/teamtag/list?ssoToken=${token}&teamId=${teamId}`);
                    if (response.status === 200) {
                        setTags(response.data);
                    }
                }
            } catch (error) {
                console.error("Error initializing Teams or fetching tags:", error);
            }
        };

        initTeams();
    }, []);

    const handleConfigureTags = () => {
        navigate("/manage-tags");
    };

    const handleSendTeams = () => {
        if (!question || !selectedTag) {
            alert("Please enter a question and select a topic.");
            return;
        }
        console.log("Sending via Teams", { question, selectedTag, onlyOnline });
        alert("Question sent via Teams!");
    };

    const handleSendEmail = () => {
        if (!question || !selectedTag) {
            alert("Please enter a question and select a topic.");
            return;
        }
        console.log("Sending via Email", { question, selectedTag, onlyOnline });
        alert("Question sent via Email!");
    };

    return (
        <div className="enableer-dashboard">
            <div className="enableer-header">
                <button className="configure-button" onClick={handleConfigureTags}>
                    Configure Tags
                </button>
            </div>
            
            <div className="enableer-content">
                <div className="enableer-logo">Enableer</div>
                
                <div className="enableer-input-container">
                    <textarea 
                        className="enableer-textarea" 
                        placeholder="Ask a question about a topic..." 
                        value={question}
                        onChange={(e) => setQuestion(e.target.value)}
                    />
                    
                    <div className="enableer-controls">
                        <div className="enableer-controls-left">
                            <select 
                                className="enableer-select"
                                value={selectedTag} 
                                onChange={(e) => setSelectedTag(e.target.value)}
                            >
                                <option value="" disabled>Select a topic</option>
                                {tags.map(tag => (
                                    <option key={tag.id} value={tag.id}>{tag.displayName}</option>
                                ))}
                            </select>
                            
                            <label className="enableer-checkbox-label">
                                <input 
                                    type="checkbox" 
                                    checked={onlyOnline} 
                                    onChange={(e) => setOnlyOnline(e.target.checked)}
                                />
                                Only online users
                            </label>
                        </div>
                        
                        <div className="enableer-actions">
                            <button className="enableer-button enableer-button-primary" onClick={handleSendTeams}>
                                Send via Teams
                            </button>
                            <button className="enableer-button enableer-button-secondary" onClick={handleSendEmail}>
                                Send via Email
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default Dashboard;
