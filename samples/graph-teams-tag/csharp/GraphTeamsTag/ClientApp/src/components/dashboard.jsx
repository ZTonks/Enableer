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
    const [selectedTags, setSelectedTags] = useState([]);
    const [onlyOnline, setOnlyOnline] = useState(false);
    const [tags, setTags] = useState([]);
    const [teamId, setTeamId] = useState("");
    const [userId, setUserId] = useState("");
    const [token, setToken] = useState("");
    const [targetType, setTargetType] = useState("1");
    const [questionHistory, setQuestionHistory] = useState([]);
    
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

    const handleLeaderboard = () => {
        navigate("/leaderboard");
    };

    const sendQuestion = async (isEmail) => {
        if (!question || !selectedTags || !selectedTags.length || !subject) {
            alert("Please enter a subject, question, and select at least one topic.");
            return;
        }

        // Convert selectedTags (array of IDs) to TagDto objects
        const tagDtos = selectedTags.map(tagId => {
            const tag = tags.find(t => t.id === tagId);
            return {
                Id: tagId,
                Name: tag.displayName
            };
        });

        const payload = {
            Tags: tagDtos,
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
            } else if (response.status === 400) {
                alert(response.data.problem);
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
                <button className="configure-button" onClick={handleLeaderboard} style={{marginRight: '10px'}}>
                    Leaderboard
                </button>
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
                                multiple
                                value={selectedTags}
                                onChange={(e) => {
                                    const selectedValues = Array.from(e.target.selectedOptions, option => option.value);
                                    setSelectedTags(selectedValues);
                                    
                                    // Fetch history for the first selected tag
                                    if (selectedValues.length > 0) {
                                        const tagId = selectedValues[0];
                                        axios.get(`/api/history/by-tag/${tagId}`)
                                            .then(res => setQuestionHistory(res.data))
                                            .catch(err => console.error(err));
                                    } else {
                                        setQuestionHistory([]);
                                    }
                                }}
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
                
                {questionHistory.length > 0 && (
                    <div style={{ marginTop: '20px', width: '100%' }}>
                        <h3 style={{ marginBottom: '10px' }}>Previous Questions</h3>
                        {questionHistory.map((q) => (
                            <div key={q.id} style={{ 
                                padding: '15px', 
                                marginBottom: '10px', 
                                backgroundColor: 'white', 
                                borderRadius: '8px',
                                border: '1px solid #ddd',
                                cursor: 'pointer'
                            }} onClick={() => {
                                // Toggle details or similar if needed, for now just show content
                            }}>
                                <div style={{ fontWeight: 'bold' }}>{q.questionTopic}</div>
                                <div style={{ marginTop: '5px' }}>{q.questionContent}</div>
                                {q.chatWebUrl && (
                                    <a 
                                        href={q.chatWebUrl} 
                                        target="_blank" 
                                        rel="noopener noreferrer"
                                        className="enableer-button enableer-button-primary"
                                        style={{ 
                                            display: 'inline-block', 
                                            marginTop: '10px', 
                                            textDecoration: 'none', 
                                            fontSize: '0.9rem',
                                            padding: '5px 10px'
                                        }}
                                        onClick={(e) => e.stopPropagation()}
                                    >
                                        Join Chat
                                    </a>
                                )}
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
};

export default Dashboard;
