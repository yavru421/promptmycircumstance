window.zlaInterop = {
    // Scene 3: Draw Wireframe Jig
    drawWireframeJig: function (elementId) {
        const el = document.getElementById(elementId);
        if (!el) return;
        
        // SVG wireframe animation logic
        el.innerHTML = `
            <svg width="100%" height="100%" viewBox="0 0 100 100" preserveAspectRatio="none">
                <path d="M10,90 L50,10 L90,90 Z" fill="none" stroke="#00f0ff" stroke-width="2" stroke-dasharray="300" stroke-dashoffset="300">
                    <animate attributeName="stroke-dashoffset" from="300" to="0" dur="2s" fill="freeze" />
                </path>
                <circle cx="50" cy="50" r="20" fill="none" stroke="#39ff14" stroke-width="1" stroke-dasharray="150" stroke-dashoffset="150">
                    <animate attributeName="stroke-dashoffset" from="150" to="0" dur="1.5s" begin="0.5s" fill="freeze" />
                </circle>
            </svg>
        `;
    },

    // Scene 4: Slider/Drag Logic
    initPourSlider: function (dotNetHelper, elementId) {
        const el = document.getElementById(elementId);
        if (!el) return;

        let isDragging = false;
        
        el.addEventListener('mousedown', (e) => {
            isDragging = true;
        });
        
        el.addEventListener('touchstart', (e) => {
            isDragging = true;
        }, { passive: true });

        window.addEventListener('mouseup', () => {
            isDragging = false;
        });
        
        window.addEventListener('touchend', () => {
            isDragging = false;
        });

        const updatePosition = (clientX) => {
            if (!isDragging) return;
            const rect = el.parentElement.getBoundingClientRect();
            let percentage = ((clientX - rect.left) / rect.width) * 100;
            percentage = Math.max(0, Math.min(100, percentage));
            el.style.left = percentage + '%';
            
            // Call back to Blazor
            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('UpdatePourState', percentage);
            }
        };

        window.addEventListener('mousemove', (e) => {
            updatePosition(e.clientX);
        });
        
        window.addEventListener('touchmove', (e) => {
            if (e.touches.length > 0) {
                updatePosition(e.touches[0].clientX);
            }
        }, { passive: true });
    },

    // Scene 5: Celebration
    triggerConfetti: function (elementId) {
        const el = document.getElementById(elementId);
        if (!el) return;

        const canvas = document.createElement('canvas');
        canvas.style.position = 'absolute';
        canvas.style.top = '0';
        canvas.style.left = '0';
        canvas.style.width = '100%';
        canvas.style.height = '100%';
        canvas.style.pointerEvents = 'none';
        canvas.style.zIndex = '9999';
        el.appendChild(canvas);

        const ctx = canvas.getContext('2d');
        canvas.width = el.clientWidth;
        canvas.height = el.clientHeight;

        const particles = [];
        for (let i = 0; i < 150; i++) {
            particles.push({
                x: canvas.width / 2,
                y: canvas.height / 2,
                vx: (Math.random() - 0.5) * 15,
                vy: (Math.random() - 0.5) * 15 - 5,
                size: Math.random() * 6 + 4,
                color: Math.random() > 0.5 ? '#00f0ff' : (Math.random() > 0.5 ? '#39ff14' : '#ff0055')
            });
        }

        let animationFrame;
        function render() {
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            particles.forEach(p => {
                p.x += p.vx;
                p.y += p.vy;
                p.vy += 0.3; // gravity
                ctx.fillStyle = p.color;
                ctx.fillRect(p.x, p.y, p.size, p.size);
            });
            animationFrame = requestAnimationFrame(render);
        }
        render();

        setTimeout(() => {
            cancelAnimationFrame(animationFrame);
            if (canvas.parentNode) {
                canvas.parentNode.removeChild(canvas);
            }
        }, 4000);
    },

    // Scroll snapping helper
    scrollToElement: function (elementId) {
        const el = document.getElementById(elementId);
        if (el) {
            el.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    },

    // Dynamic certificate generator & downloader
    downloadCertificate: function (tier, score, challengeTitle, stars) {
        const canvas = document.createElement('canvas');
        canvas.width = 800;
        canvas.height = 600;
        const ctx = canvas.getContext('2d');

        // Draw Dark Background
        ctx.fillStyle = '#0a0a0c';
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        // Draw Neon Borders
        ctx.strokeStyle = '#00f0ff';
        ctx.lineWidth = 4;
        ctx.strokeRect(20, 20, canvas.width - 40, canvas.height - 40);

        ctx.strokeStyle = '#ff0055';
        ctx.lineWidth = 1;
        ctx.strokeRect(25, 25, canvas.width - 50, canvas.height - 50);

        // Draw Title
        ctx.fillStyle = '#ffffff';
        ctx.font = 'bold 32px sans-serif';
        ctx.textAlign = 'center';
        ctx.fillText('THE PROMPT CRUCIBLE', canvas.width / 2, 100);

        ctx.fillStyle = '#00f0ff';
        ctx.font = '16px monospace';
        ctx.fillText('OPERATOR CERTIFICATE OF COMPETENCY', canvas.width / 2, 130);

        // Draw Line
        ctx.strokeStyle = 'rgba(0, 240, 255, 0.3)';
        ctx.beginPath();
        ctx.moveTo(100, 160);
        ctx.lineTo(700, 160);
        ctx.stroke();

        // Draw Level/Challenge Info
        ctx.fillStyle = '#888888';
        ctx.font = '16px sans-serif';
        ctx.fillText('For successfully executing the scenario:', canvas.width / 2, 210);

        ctx.fillStyle = '#ffffff';
        ctx.font = 'bold 22px sans-serif';
        ctx.fillText(challengeTitle.toUpperCase(), canvas.width / 2, 240);

        // Draw Score Info
        ctx.fillStyle = '#888888';
        ctx.font = '16px sans-serif';
        ctx.fillText('Achieving a Performance Score of:', canvas.width / 2, 300);

        ctx.fillStyle = '#39ff14'; // matrix green
        ctx.font = 'bold 48px sans-serif';
        ctx.fillText(score + ' / 100', canvas.width / 2, 355);

        // Draw Tier Info
        ctx.fillStyle = '#ffffff';
        ctx.font = 'bold 20px monospace';
        ctx.fillText(tier.toUpperCase(), canvas.width / 2, 420);

        // Draw Rating (Stars)
        ctx.fillStyle = '#ffbf00'; // Amber
        ctx.font = '24px sans-serif';
        ctx.fillText('★'.repeat(stars) + '☆'.repeat(Math.max(0, 3 - stars)), canvas.width / 2, 460);

        // Draw Footer/Date
        const dateStr = new Date().toLocaleDateString();
        ctx.fillStyle = '#555555';
        ctx.font = '12px monospace';
        ctx.fillText('VERIFIED SYSTEM RUN: ' + dateStr, canvas.width / 2, 520);
        ctx.fillText('SECURE CONTAINER COMPLETED IN BROWSER MEMORY', canvas.width / 2, 545);

        // Trigger PDF Download using jsPDF
        try {
            const { jsPDF } = window.jspdf;
            const pdf = new jsPDF({
                orientation: 'landscape',
                unit: 'px',
                format: [800, 600]
            });
            const imgData = canvas.toDataURL('image/jpeg', 0.95);
            pdf.addImage(imgData, 'JPEG', 0, 0, 800, 600);
            pdf.save('Prompt_Crucible_Certificate.pdf');
        } catch (e) {
            console.error('jsPDF error, falling back to PNG:', e);
            const dataUrl = canvas.toDataURL('image/png');
            const link = document.createElement('a');
            link.download = 'Prompt_Crucible_Certificate.png';
            link.href = dataUrl;
            link.click();
        }
    }
};
