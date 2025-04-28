// Common functionality
document.addEventListener('DOMContentLoaded', () => {
    // 0) Check localStorage for theme preference
    const storedTheme = localStorage.getItem('theme');
    if (storedTheme === 'light') {
        document.documentElement.classList.add('light-mode');
    }

    // Ripple effect on buttons
    document.querySelectorAll('.btn').forEach(btn => {
        btn.addEventListener('click', createRipple);
    });

    // THEME TOGGLE
    const themeToggleBtn = document.getElementById('themeToggle');
    if (themeToggleBtn) {
        themeToggleBtn.addEventListener('click', () => {
            const isNowLight = document.documentElement.classList.toggle('light-mode');
            if (isNowLight) {
                localStorage.setItem('theme', 'light');
            } else {
                localStorage.removeItem('theme');
            }
        });
    }

    // Booking functionality (quantity selectors)
    document.querySelectorAll('.quantity-selector').forEach(selector => {
        const plus = selector.querySelector('.plus');
        const minus = selector.querySelector('.minus');
        const count = selector.querySelector('.count');

        plus.addEventListener('click', () => {
            count.textContent = parseInt(count.textContent) + 1;
            updateOrderSummary();
        });

        minus.addEventListener('click', () => {
            if (parseInt(count.textContent) > 0) {
                count.textContent = parseInt(count.textContent) - 1;
                updateOrderSummary();
            }
        });
    });

    // Filter functionality for events
    const priceRange = document.querySelector('input[type="range"]');
    if (priceRange) {
        const priceDisplay = document.createElement('div');
        priceDisplay.className = 'text-end text-muted small mb-2';
        priceRange.parentElement.prepend(priceDisplay);

        priceRange.addEventListener('input', () => {
            priceDisplay.textContent = `$${priceRange.value}`;
        });
    }

    document.querySelectorAll('.form-check-input').forEach(checkbox => {
        checkbox.addEventListener('change', filterEvents);
    });

    // Only apply fake form submission to forms with .js-validate class
    document.querySelectorAll('form.js-validate').forEach(form => {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            const inputs = this.querySelectorAll('input, textarea');
            let isValid = true;

            inputs.forEach(input => {
                if (!input.checkValidity()) {
                    isValid = false;
                    input.classList.add('is-invalid');
                } else {
                    input.classList.remove('is-invalid');
                }
            });

            if (isValid) {
                showLoadingSpinner();
                // Simulate API call
                setTimeout(() => {
                    window.location.href = 'confirmation.html';
                }, 1500);
            }
        });
    });

    // Login and signup form handling (static pages)
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', handleLogin);
    }

    const signupForm = document.getElementById('signupForm');
    if (signupForm) {
        signupForm.addEventListener('submit', handleSignup);
    }
});

// Ripple effect function
function createRipple(e) {
    const ripple = document.createElement('div');
    ripple.className = 'ripple-effect';
    ripple.style.left = `${e.offsetX}px`;
    ripple.style.top = `${e.offsetY}px`;
    this.appendChild(ripple);
    setTimeout(() => ripple.remove(), 600);
}

// Update booking order summary
function updateOrderSummary() {
    const ticketPrice = 199.99;
    const serviceFee = 40.00;
    const ticketCount = parseInt(document.querySelector('.count').textContent || "0");
    const total = (ticketCount * ticketPrice) + serviceFee;
    const totalDisplay = document.querySelector('.order-summary .total');
    if (totalDisplay) {
        totalDisplay.textContent = `$${total.toFixed(2)}`;
    }
}

// Filter events based on category
function filterEvents() {
    const activeCategories = Array.from(
        document.querySelectorAll('.form-check-input:checked')
    ).map(cb => cb.id);

    document.querySelectorAll('.event-card').forEach(card => {
        const category = card.querySelector('.badge')?.textContent.toLowerCase();
        const matches = activeCategories.length === 0 ||
            activeCategories.includes(category);
        card.style.display = matches ? 'block' : 'none';
    });
}

// Simulated login handling (for frontend static page)
function handleLogin(e) {
    e.preventDefault();
    const email = document.getElementById('email')?.value;
    const password = document.getElementById('password')?.value;
    const rememberMe = document.getElementById('rememberMe')?.checked;

    console.log('Login attempt:', { email, password, rememberMe });

    simulateLogin(email, password)
        .then(response => {
            if (response.success) {
                window.location.href = 'dashboard.html';
            } else {
                alert('Login failed: ' + response.message);
            }
        })
        .catch(error => {
            console.error('Login error:', error);
            alert('An error occurred. Please try again.');
        });
}

function simulateLogin(email, password) {
    return new Promise((resolve) => {
        setTimeout(() => {
            if (email && password) {
                resolve({ success: true, message: 'Login successful' });
            } else {
                resolve({ success: false, message: 'Invalid credentials' });
            }
        }, 1000);
    });
}

// Simulated signup handling (for frontend static page)
function handleSignup(e) {
    e.preventDefault();
    const fullName = document.getElementById('fullName')?.value;
    const email = document.getElementById('email')?.value;
    const password = document.getElementById('password')?.value;
    const confirmPassword = document.getElementById('confirmPassword')?.value;
    const agreeTerms = document.getElementById('agreeTerms')?.checked;

    if (password !== confirmPassword) {
        alert("Passwords don't match!");
        return;
    }

    if (!agreeTerms) {
        alert("Please agree to the Terms and Conditions");
        return;
    }

    console.log('Signup attempt:', { fullName, email, password, agreeTerms });

    simulateSignup(fullName, email, password)
        .then(response => {
            if (response.success) {
                alert('Signup successful! Please log in.');
                window.location.href = 'login.html';
            } else {
                alert('Signup failed: ' + response.message);
            }
        })
        .catch(error => {
            console.error('Signup error:', error);
            alert('An error occurred. Please try again.');
        });
}

function simulateSignup(fullName, email, password) {
    return new Promise((resolve) => {
        setTimeout(() => {
            if (fullName && email && password) {
                resolve({ success: true, message: 'Signup successful' });
            } else {
                resolve({ success: false, message: 'Invalid information' });
            }
        }, 1000);
    });
}

function showLoadingSpinner() {
    // You can implement a loading spinner here if you want
    console.log('Showing loading spinner...');
}
