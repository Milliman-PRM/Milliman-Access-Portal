import * as DOMPurify from 'dompurify';
import * as marked from 'marked';

export function convertMarkdownToHTML(rawMarkdown: string): string {
  const markedOptions = {
    breaks: true,
    gfm: true,
    sanitize: false,
  };
  const rawHTML = marked(rawMarkdown.trim(), markedOptions);
  const sanitizedHTML = DOMPurify.sanitize(rawHTML);
  return sanitizedHTML;
}
