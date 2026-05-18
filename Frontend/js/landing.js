(function () {
  "use strict";

  const page = document.querySelector(".lp-page");
  const langButton = document.getElementById("lp-lang-btn");
  const demoButton = document.getElementById("lp-demo-btn");
  const flag = document.getElementById("lp-flag");
  const label = document.getElementById("lp-lang-label");
  const sceneEl = document.querySelector(".lp-scene");
  const gridEl = document.querySelector(".lp-grid");
  const stackWrap = document.querySelector(".lp-stack-wrap");
  const stackItems = document.querySelectorAll(".lp-stack span");
  const motionQuery = window.matchMedia("(prefers-reduced-motion: reduce)");
  const compactQuery = window.matchMedia("(max-width: 768px)");

  const isMobileView = function () {
    return compactQuery.matches;
  };
  const isReducedMotion = function () {
    return motionQuery.matches;
  };

  function getLanguage() {
    try {
      return localStorage.getItem("language") || "en";
    } catch {
      return "en";
    }
  }
  function saveLanguage(lang) {
    try {
      localStorage.setItem("language", lang);
    } catch {}
  }
  function setLanguageButton() {
    if (!flag || !label) return;
    if (getLanguage() === "fr") {
      flag.innerHTML =
        '<svg width="24" height="18" viewBox="0 0 24 18" aria-hidden="true"><rect width="24" height="18" fill="#fff"/><rect width="6" height="18" fill="#D80621"/><rect x="18" width="6" height="18" fill="#D80621"/><path d="M12 3l1 3h3l-2.5 2 1 3L12 9.5 9.5 11l1-3L8 6h3z" fill="#D80621"/></svg>';
      label.textContent = "EN";
    } else {
      flag.innerHTML =
        '<svg width="24" height="18" viewBox="0 0 24 18" aria-hidden="true"><rect width="24" height="18" fill="#003DA5"/><rect x="11" width="2" height="18" fill="#fff"/><rect y="8" width="24" height="2" fill="#fff"/><circle cx="6" cy="4.5" r="1.5" fill="#fff"/><circle cx="18" cy="4.5" r="1.5" fill="#fff"/><circle cx="6" cy="13.5" r="1.5" fill="#fff"/><circle cx="18" cy="13.5" r="1.5" fill="#fff"/></svg>';
      label.textContent = "FR";
    }
  }
  function toggleLanguage() {
    const nextLang = getLanguage() === "fr" ? "en" : "fr";
    if (typeof setLanguage === "function") {
      setLanguage(nextLang);
    } else {
      saveLanguage(nextLang);
    }
    window.location.reload();
  }

  function scrambleText(el, finalText, duration) {
    if (isReducedMotion()) {
      el.textContent = finalText;
      return;
    }
    const chars =
      "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#$%";
    const steps = Math.floor(duration / 40);
    let step = 0;
    const interval = setInterval(function () {
      el.textContent = finalText
        .split("")
        .map(function (c, i) {
          if (c === " ") return " ";
          if (i < Math.floor((step / steps) * finalText.length)) return c;
          return chars[Math.floor(Math.random() * chars.length)];
        })
        .join("");
      step++;
      if (step > steps) {
        el.textContent = finalText;
        clearInterval(interval);
      }
    }, 40);
  }

  function initCursor() {
    if (isMobileView() || isReducedMotion()) return;
    const cursor = document.createElement("div");
    cursor.id = "lp-cursor";
    cursor.style.cssText = [
      "position:fixed",
      "top:0",
      "left:0",
      "width:12px",
      "height:12px",
      "border-radius:50%",
      "background:rgba(79,134,198,0.9)",
      "box-shadow:0 0 16px rgba(79,134,198,0.8),0 0 32px rgba(79,134,198,0.4)",
      "pointer-events:none",
      "z-index:9999",
      "transform:translate(-50%,-50%)",
      "transition:width 0.2s,height 0.2s,opacity 0.2s",
      "mix-blend-mode:screen",
    ].join(";");
    document.body.appendChild(cursor);
    const trail = [];
    for (let i = 0; i < 8; i++) {
      const dot = document.createElement("div");
      const size = 8 - i;
      dot.style.cssText = [
        "position:fixed",
        "top:0",
        "left:0",
        `width:${size}px`,
        `height:${size}px`,
        "border-radius:50%",
        `background:rgba(79,134,198,${0.5 - i * 0.05})`,
        "pointer-events:none",
        `z-index:${9998 - i}`,
        "transform:translate(-50%,-50%)",
      ].join(";");
      document.body.appendChild(dot);
      trail.push({ el: dot, x: 0, y: 0 });
    }
    let cx = 0,
      cy = 0;
    window.addEventListener("mousemove", function (e) {
      cx = e.clientX;
      cy = e.clientY;
      cursor.style.left = cx + "px";
      cursor.style.top = cy + "px";
    });
    (function animTrail() {
      requestAnimationFrame(animTrail);
      let px = cx,
        py = cy;
      trail.forEach(function (t) {
        t.x += (px - t.x) * 0.35;
        t.y += (py - t.y) * 0.35;
        t.el.style.left = t.x + "px";
        t.el.style.top = t.y + "px";
        px = t.x;
        py = t.y;
      });
    })();
    document.addEventListener("mousedown", function () {
      cursor.style.width = "20px";
      cursor.style.height = "20px";
    });
    document.addEventListener("mouseup", function () {
      cursor.style.width = "12px";
      cursor.style.height = "12px";
    });
  }

  function startGsap() {
    if (isReducedMotion() || !window.gsap) return;
    const gsap = window.gsap;
    setTimeout(function () {
      scrambleText(document.getElementById("lp-title"), "BankLite", 1400);
    }, 200);
    const tl = gsap.timeline({ defaults: { ease: "power4.out" } });
    tl.from(".lp-page h1", { duration: 1.1, y: 54, opacity: 0, scale: 0.92 })
      .from(".lp-actions", { duration: 0.75, y: 18, opacity: 0 }, "-=0.45")
      .from(
        ".lp-stack-wrap",
        { duration: 0.78, y: 18, opacity: 0, scale: 0.96 },
        "-=0.35",
      )
      .from(
        ".lp-stack span",
        { duration: 0.52, y: 12, opacity: 0, stagger: 0.04 },
        "-=0.45",
      )
      .from(
        ".lp-showcase",
        { duration: 1.1, x: 60, opacity: 0, scale: 0.88, rotate: -4 },
        "-=0.95",
      );
    if (!isMobileView()) {
      gsap.to(".lp-btn-signin", {
        boxShadow: "0 24px 64px rgba(0,0,0,0.34),0 0 70px rgba(79,134,198,0.6)",
        duration: 2.2,
        repeat: -1,
        yoyo: true,
        ease: "sine.inOut",
        delay: 1.6,
      });
      gsap.to(".lp-page h1", {
        textShadow:
          "0 0 60px rgba(127,176,232,0.9),0 26px 70px rgba(0,0,0,0.3)",
        duration: 2.8,
        repeat: -1,
        yoyo: true,
        ease: "sine.inOut",
        delay: 1.2,
      });
    }
  }

  function bindTilt() {
    if (isReducedMotion()) return;
    function sync(clientX, clientY) {
      if (!page) return;
      const isMobile = isMobileView();
      const x = clientX / window.innerWidth - 0.5;
      const y = clientY / window.innerHeight - 0.5;
      const rs = isMobile ? 4 : 8;
      const ss = isMobile ? 10 : 22;
      const gs = isMobile ? 12 : 30;
      const sx = x * ss,
        sy = y * ss;
      page.style.setProperty("--page-rot-x", `${-y * rs}deg`);
      page.style.setProperty("--page-rot-y", `${x * rs}deg`);
      page.style.setProperty("--page-shift-x", `${sx}px`);
      page.style.setProperty("--page-shift-y", `${sy}px`);
      page.style.setProperty("--page-shift-x-soft", `${sx * -0.55}px`);
      page.style.setProperty("--page-shift-y-soft", `${sy * -0.55}px`);
      page.style.setProperty("--grid-x", `${x * gs}px`);
      page.style.setProperty("--grid-y", `${y * gs}px`);
      if (sceneEl) {
        const sx2 = x * (isMobile ? 18 : 34);
        const sy2 = y * (isMobile ? 14 : 28);
        sceneEl.style.setProperty("--scene-x", `${sx2}px`);
        sceneEl.style.setProperty("--scene-y", `${sy2}px`);
        sceneEl.style.setProperty("--scene-x-soft", `${sx2 * -0.7}px`);
        sceneEl.style.setProperty("--scene-y-soft", `${sy2 * -0.7}px`);
      }
      if (gridEl) {
        gridEl.style.setProperty("--grid-x", `${x * gs}px`);
        gridEl.style.setProperty("--grid-y", `${y * gs}px`);
      }
    }
    function reset() {
      [page, sceneEl, gridEl].forEach(function (el) {
        if (!el) return;
        [
          "--page-rot-x",
          "--page-rot-y",
          "--page-shift-x",
          "--page-shift-y",
          "--page-shift-x-soft",
          "--page-shift-y-soft",
          "--grid-x",
          "--grid-y",
          "--scene-x",
          "--scene-y",
          "--scene-x-soft",
          "--scene-y-soft",
        ].forEach(function (p) {
          el.style.removeProperty(p);
        });
      });
    }
    window.addEventListener("pointermove", function (e) {
      sync(e.clientX, e.clientY);
    });
    window.addEventListener("pointerleave", reset);
    if (stackWrap) {
      stackWrap.addEventListener("pointerdown", function (e) {
        stackItems.forEach(function (i) {
          i.classList.remove("is-active");
        });
        if (e.target.matches(".lp-stack span"))
          e.target.classList.add("is-active");
      });
      stackWrap.addEventListener("pointermove", function (e) {
        const b = stackWrap.getBoundingClientRect();
        const x = (e.clientX - b.left) / b.width - 0.5;
        const y = (e.clientY - b.top) / b.height - 0.5;
        stackWrap.style.setProperty(
          "--stack-x",
          `${Math.round((x + 0.5) * 100)}%`,
        );
        stackWrap.style.setProperty(
          "--stack-y",
          `${Math.round((y + 0.5) * 100)}%`,
        );
        if (e.target.matches(".lp-stack span")) {
          stackItems.forEach(function (i) {
            i.classList.remove("is-active");
          });
          e.target.classList.add("is-active");
        }
        if (!isMobileView()) {
          stackWrap.style.setProperty("--stack-rot-x", `${-y * 6}deg`);
          stackWrap.style.setProperty("--stack-rot-y", `${x * 7}deg`);
          stackItems.forEach(function (item) {
            const d = Number(item.dataset.depth || 18);
            item.style.setProperty("--chip-x", `${x * d}px`);
            item.style.setProperty("--chip-y", `${y * d * 0.7}px`);
          });
        }
      });
      function resetStack() {
        ["--stack-rot-x", "--stack-rot-y", "--stack-x", "--stack-y"].forEach(
          function (p) {
            stackWrap.style.removeProperty(p);
          },
        );
        stackItems.forEach(function (item) {
          item.classList.remove("is-active");
          item.style.removeProperty("--chip-x");
          item.style.removeProperty("--chip-y");
        });
      }
      stackWrap.addEventListener("pointerup", resetStack);
      stackWrap.addEventListener("pointercancel", resetStack);
      stackWrap.addEventListener("pointerleave", resetStack);
    }
  }

  function initVanta() {
    if (isReducedMotion() || !window.VANTA || !window.VANTA.NET) return;
    const isMobile = isMobileView();
    window.VANTA.NET({
      el: ".lp-page",
      THREE: window.THREE,
      color: 0x6fa7df,
      backgroundColor: 0x0a1628,
      points: isMobile ? 10 : 16,
      maxDistance: isMobile ? 22 : 27,
      spacing: isMobile ? 18 : 16,
      showDots: true,
      mouseControls: true,
      touchControls: true,
      gyroControls: false,
    });
  }

  function bindShowcaseCard() {
    const cardScene = document.querySelector(".lp-card-scene");
    const card = document.querySelector(".lp-credit-card");

    if (!cardScene || !card || isReducedMotion()) return;

    function syncCard(clientX, clientY) {
      const isMobile = isMobileView();
      const bounds = cardScene.getBoundingClientRect();
      const x = (clientX - bounds.left) / bounds.width - 0.5;
      const y = (clientY - bounds.top) / bounds.height - 0.5;
      const rotX = isMobile ? -y * 5 : -y * 10;
      const rotY = isMobile ? x * 6 : x * 12;

      cardScene.style.setProperty("--card-rot-x", `${rotX}deg`);
      cardScene.style.setProperty("--card-rot-y", `${rotY}deg`);
      card.style.setProperty("--card-shine", `${x * 22}%`);
    }

    function resetCard() {
      cardScene.style.removeProperty("--card-rot-x");
      cardScene.style.removeProperty("--card-rot-y");
      card.style.removeProperty("--card-shine");
    }

    cardScene.addEventListener("pointermove", function (e) {
      syncCard(e.clientX, e.clientY);
    });
    cardScene.addEventListener("pointerleave", resetCard);
    cardScene.addEventListener("pointercancel", resetCard);
  }
  setLanguageButton();
  if (langButton) langButton.addEventListener("click", toggleLanguage);
  if (demoButton) {
    demoButton.addEventListener("click", function () {
      window.location.href = "index.html";
    });
  }
  initCursor();
  initVanta();

  window.addEventListener("load", function () {
    bindShowcaseCard();
    startGsap();
    bindTilt();
  });
})();
