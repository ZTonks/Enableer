// <copyright file="dashboard.jsx" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
// </copyright>

import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import * as microsoftTeams from "@microsoft/teams-js";
import axios from "axios";
import "../style/style.css";
import logo from "../assets/logo.png";

const Dashboard = () => {
    const navigate = useNavigate();
    const [question, setQuestion] = useState("");
    const [subject, setSubject] = useState("");
    const [selectedTag, setSelectedTag] = useState("");
    const [onlyOnline, setOnlyOnline] = useState(false);
    const [tags, setTags] = useState([]);
    const [teamId, setTeamId] = useState("");
    const [userId, setUserId] = useState("");
    const [token, setToken] = useState("");
    const [targetType, setTargetType] = useState("1");
    
    useEffect(() => {
        const initTeams = async () => {
            try {
                await microsoftTeams.app.initialize();
                microsoftTeams.app.notifySuccess();
                
                const context = await microsoftTeams.app.getContext();
                const authToken = await microsoftTeams.authentication.getAuthToken();
                const currentTeamId = context.team.groupId;
                const currentUserId = context.user.id;

                setTeamId(currentTeamId);
                setUserId(currentUserId);
                setToken(authToken);
                
                if (currentTeamId && authToken) {
                    const response = await axios.get(`/api/teamtag/list?ssoToken=${authToken}&teamId=${currentTeamId}`);
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

    const sendQuestion = async (isEmail) => {
        if (!question || !selectedTag || !subject) {
            alert("Please enter a subject, question, and select a topic.");
            return;
        }

        const payload = {
            Tag: selectedTag,
            QuestionTopic: subject,
            Question: question,
            TeamId: teamId,
            TargetsOnlineUsers: onlyOnline,
            Email: isEmail,
            QuestionTarget: parseInt(targetType),
            RequesterUserId: userId
        };

        try {
            console.log(`Sending via ${isEmail ? "Email" : "Teams"}`, payload);
            const response = await axios.post(`/api/questions?ssoToken=${token}`, payload);
            
            if (response.status === 200) {
                alert(`Question sent via ${isEmail ? "Email" : "Teams"}!`);
                setQuestion("");
                setSubject("");
            } else {
                alert("Failed to send question.");
            }
        } catch (error) {
            console.error("Error sending question:", error);
            alert("Error sending question. Please try again.");
        }
    };

    const handleSendTeams = () => {
        sendQuestion(false);
    };

    const handleSendEmail = () => {
        sendQuestion(true);
    };

    return (
        <div className="enableer-dashboard">
            <div className="enableer-header">
                <button className="configure-button" onClick={handleConfigureTags}>
                    Configure Tags
                </button>
            </div>
            
            <div className="enableer-content">
                <img src={logo} alt="Enableer Logo" style={{ maxWidth: "200px", marginBottom: "20px" }} />
                <div className="enableer-logo">Enableer</div>
                
                <div className="enableer-input-container">
                    <input
                        className="enableer-text-input"
                        placeholder="Subject"
                        value={subject}
                        onChange={(e) => setSubject(e.target.value)}
                    />
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

                            <select
                                className="enableer-select"
                                value={targetType}
                                onChange={(e) => setTargetType(e.target.value)}
                            >
                                <option value="1">All people with tag</option>
                                <option value="0">One person with tag</option>
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
