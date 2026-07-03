// Discreet personal-links footer shared by the Index / Manage / About sheets.
// The index-reveal class joins the page's GSAP stagger where one runs (Index,
// About) and is inert elsewhere — there is no hidden base style behind it.
export function SocialFooter() {
  return (
    <footer className="social-footer index-reveal">
      <a href="https://github.com/koppelbw" target="_blank" rel="noreferrer">
        GitHub
      </a>
      <span aria-hidden="true">·</span>
      <a href="https://www.linkedin.com/in/william-koppelberger-5405905a/" target="_blank" rel="noreferrer">
        LinkedIn
      </a>
      <span aria-hidden="true">·</span>
      <a href="https://proud-pond-097c6f40f.7.azurestaticapps.net/" target="_blank" rel="noreferrer">
        Portfolio
      </a>
    </footer>
  );
}
