# Quantower-Public

## Econophysics Indicators

### Hurst Exponent Indicator
Derives from studying how particles move in physics (Brownian motion). Measures market persistence and mean reversion tendencies using fractal analysis. Values above 0.5 indicate trending markets where momentum based strategies work best, values below 0.5 suggest mean reversion conditions.

### Entropy of Returns Indicator
Quantifies market predictability and chaos using information theory. Low entropy indicates structured, predictable price movements where other indicators are more reliable. High entropy signals chaotic markets where you should reduce position sizes and wait for clearer signals.

### Multiscale Volatility Indicator
Analyzes volatility across multiple time periods simultaneously to detect hidden market stress. When short term volatility rises faster than long term volatility, it often signals impending breakouts or major moves before they appear in price action.

### Volume Acceleration Indicator
Based on Newton's second law. If volume = mass and price = velocity, then volume acceleration represents the "force" behind market moves. Calculates the rate of change in volume acceleration to detect sudden shifts in market interest. Large positive spikes often precede breakouts, while negative acceleration during price moves can signal momentum exhaustion and potential reversals.

### Quantum Tunneling Indicator
Applies quantum mechanics principles to predict breakthrough probability at support/resistance levels. Calculates tunneling probability based on price proximity to barriers and underlying market energy states.

**Formulas:**
- Energy = (0.4 × momentum) + (0.4 × volume_ratio) + (0.2 × volatility)
- P = e^(-2κa) × distance_factor
- Where: κ = sq(2m(V-E))
- V = Barrier strength (0-1)
- E = Market energy (0-1)
- m = Market mass (inertia)
- a = Barrier width (ATR normalized)

**Signal Interpretation:**
- Cyan > .7 = High breakthrough probability
- Cyan < .3 = Lower breakthrough probability
- Red level = if market has enough Energy

## Volume Analysis

### Cumulative Delta Indicator with EMA
Added an EMA to the default Quantower CVD Indicator.

## Economic Data

### Economic Events Indicator
Displays daily/weekly economic calendar events on your charts by scraping data from Forex Factory. Shows high, medium, and low impact events with customizable filtering by currency and impact level.

## Data Export

### Export Renko Data Indicator
Enables exporting renko chart data to CSV. Something not currently supported by Quantower, which only allows exporting data from the base timeframe used to calculate the bricks, rather than the aggregated renko bricks themselves. Includes OHLC, volume, and build time information for each Renko brick.

## Kernel Smoothers

### Gaussian Kernel Smoother
Applies Gaussian weighting to smooth price data while preserving important trend information. The Gaussian kernel naturally emphasizes recent data while gradually reducing the influence of older data points, creating smooth trend lines without excessive lag.

### Levy Kernel Smoother
Uses Levy distribution weighting for price smoothing, which is particularly effective for financial data that exhibits fat-tailed distributions. This smoother can better handle extreme price movements while maintaining responsiveness to genuine trend changes.

### Periodic Kernel Smoother
Applies periodic kernel functions designed to capture cyclical patterns in price data. Useful for markets with known seasonal or cyclical components, as it can emphasize recurring patterns while smoothing out random noise.

### Rational Quadratic Kernel Smoother
Combines the benefits of polynomial and exponential smoothing using rational quadratic kernel functions.

## Custom Indicators

### Saurabh Indicator
A multi-timeframe indicator combining Fair Value Gaps (FVG), EMAs, VWAP, Supertrend, ADX, and RSI into a single trading system. Developed as freelance project.