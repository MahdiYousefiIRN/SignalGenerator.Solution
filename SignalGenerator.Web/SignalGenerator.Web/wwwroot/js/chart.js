// Global variable to store the chart instance
let signalChart;

/**
 * Renders or updates a signal chart using Chart.js.
 * @param {Array} labels - The labels for the X-axis.
 * @param {Array} data - The data points for the signal strength.
 */
function renderSignalChart(labels, data) {
    try {
        // Check if canvas exists
        const canvas = document.getElementById('signalsChart');
        if (!canvas) {
            console.warn("⚠️ Warning: 'signalsChart' element not found in the DOM.");
            return;
        }

        const ctx = canvas.getContext('2d');

        // Validate data input
        if (!Array.isArray(labels) || !Array.isArray(data)) {
            console.error("❌ Error: Invalid data format. 'labels' and 'data' must be arrays.");
            return;
        }

        console.log("📊 Rendering Signal Chart...");

        // If the chart already exists, update its data
        if (signalChart) {
            console.log("🔄 Updating existing chart...");
            signalChart.data.labels = labels;
            signalChart.data.datasets[0].data = data;
            signalChart.update();
        } else {
            // Create a new chart instance
            console.log("🆕 Creating a new chart...");
            signalChart = new Chart(ctx, {
                type: 'line',
                data: {
                    labels: labels,
                    datasets: [{
                        label: '📡 Signal Strength',
                        data: data,
                        borderColor: 'rgb(75, 192, 192)', // Line color
                        backgroundColor: 'rgba(75, 192, 192, 0.2)', // Fill color
                        fill: true,
                        tension: 0.4 // Smooth curve effect
                    }]
                },
                options: {
                    responsive: true,
                    plugins: {
                        legend: { display: true } // Show legend
                    },
                    scales: {
                        x: { beginAtZero: true },
                        y: { beginAtZero: true }
                    }
                }
            });
        }

        console.log("✅ Signal Chart rendered successfully.");
    } catch (error) {
        console.error("🔥 Error rendering chart:", error);
    }
}
