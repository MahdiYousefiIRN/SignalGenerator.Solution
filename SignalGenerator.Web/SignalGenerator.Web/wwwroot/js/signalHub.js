class SignalHubClient {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 5000; // 5 seconds
        this.signalHandlers = new Set();
        this.groupHandlers = new Set();
    }

    async initialize() {
        try {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/signalHub")
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // Retry intervals in milliseconds
                .configureLogging(signalR.LogLevel.Information)
                .build();

            this.setupEventHandlers();
            await this.startConnection();
        } catch (error) {
            console.error("Error initializing SignalR connection:", error);
            this.handleConnectionError(error);
        }
    }

    setupEventHandlers() {
        this.connection.on("ReceiveSignal", (signal) => {
            this.signalHandlers.forEach(handler => handler(signal));
        });

        this.connection.on("GroupUpdate", (groupName, signal) => {
            this.groupHandlers.forEach(handler => handler(groupName, signal));
        });

        this.connection.onreconnecting((error) => {
            console.warn("SignalR reconnecting...", error);
            this.isConnected = false;
        });

        this.connection.onreconnected((connectionId) => {
            console.log("SignalR reconnected with connection ID:", connectionId);
            this.isConnected = true;
            this.reconnectAttempts = 0;
        });

        this.connection.onclose((error) => {
            console.error("SignalR connection closed:", error);
            this.isConnected = false;
            this.handleConnectionError(error);
        });
    }

    async startConnection() {
        try {
            await this.connection.start();
            console.log("SignalR connection established");
            this.isConnected = true;
            this.reconnectAttempts = 0;
        } catch (error) {
            console.error("Error starting SignalR connection:", error);
            this.handleConnectionError(error);
        }
    }

    handleConnectionError(error) {
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
            this.reconnectAttempts++;
            console.log(`Attempting to reconnect (${this.reconnectAttempts}/${this.maxReconnectAttempts})...`);
            setTimeout(() => this.startConnection(), this.reconnectDelay);
        } else {
            console.error("Max reconnection attempts reached");
            this.isConnected = false;
        }
    }

    onSignalReceived(handler) {
        this.signalHandlers.add(handler);
        return () => this.signalHandlers.delete(handler);
    }

    onGroupUpdate(handler) {
        this.groupHandlers.add(handler);
        return () => this.groupHandlers.delete(handler);
    }

    async sendSignal(signal) {
        if (!this.isConnected) {
            throw new Error("SignalR connection is not established");
        }

        try {
            await this.connection.invoke("SendSignal", signal);
        } catch (error) {
            console.error("Error sending signal:", error);
            throw error;
        }
    }

    async sendSignalToClient(connectionId, signal) {
        if (!this.isConnected) {
            throw new Error("SignalR connection is not established");
        }

        try {
            await this.connection.invoke("SendSignalToClient", connectionId, signal);
        } catch (error) {
            console.error("Error sending signal to client:", error);
            throw error;
        }
    }

    async sendSignalToGroup(groupName, signal) {
        if (!this.isConnected) {
            throw new Error("SignalR connection is not established");
        }

        try {
            await this.connection.invoke("SendSignalToGroup", groupName, signal);
        } catch (error) {
            console.error("Error sending signal to group:", error);
            throw error;
        }
    }

    async joinGroup(groupName) {
        if (!this.isConnected) {
            throw new Error("SignalR connection is not established");
        }

        try {
            await this.connection.invoke("AddToGroup", groupName);
        } catch (error) {
            console.error("Error joining group:", error);
            throw error;
        }
    }

    async leaveGroup(groupName) {
        if (!this.isConnected) {
            throw new Error("SignalR connection is not established");
        }

        try {
            await this.connection.invoke("RemoveFromGroup", groupName);
        } catch (error) {
            console.error("Error leaving group:", error);
            throw error;
        }
    }

    async getRecentSignals(count = 10) {
        if (!this.isConnected) {
            throw new Error("SignalR connection is not established");
        }

        try {
            return await this.connection.invoke("GetRecentSignals", count);
        } catch (error) {
            console.error("Error getting recent signals:", error);
            throw error;
        }
    }

    async isClientActive(connectionId) {
        if (!this.isConnected) {
            throw new Error("SignalR connection is not established");
        }

        try {
            return await this.connection.invoke("IsClientActive", connectionId);
        } catch (error) {
            console.error("Error checking client activity:", error);
            throw error;
        }
    }

    disconnect() {
        if (this.connection) {
            this.connection.stop();
            this.isConnected = false;
            this.signalHandlers.clear();
            this.groupHandlers.clear();
        }
    }
}

// Create a global instance
window.signalHubClient = new SignalHubClient();

// Initialize the connection when the page loads
document.addEventListener('DOMContentLoaded', () => {
    window.signalHubClient.initialize();
}); 