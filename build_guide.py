from reportlab.lib.pagesizes import A4
from reportlab.lib.units import mm
from reportlab.lib.enums import TA_LEFT, TA_CENTER, TA_RIGHT, TA_JUSTIFY
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.colors import HexColor, white, black
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle,
    PageBreak, HRFlowable, KeepTogether
)
from reportlab.platypus.flowables import Flowable
from reportlab.pdfgen import canvas as rl_canvas

import os
from pathlib import Path

W, H = A4
MARGIN = 20 * mm
CW = W - 2 * MARGIN  # content width

# ── Palette ───────────────────────────────────────────────────────────────────
P = {
    'ink':       HexColor('#0D1117'),
    'navy':      HexColor('#0F1C35'),
    'blue':      HexColor('#1A3A6E'),
    'accent':    HexColor('#2563EB'),
    'sky':       HexColor('#3B82F6'),
    'teal':      HexColor('#0D9488'),
    'green':     HexColor('#15803D'),
    'amber':     HexColor('#B45309'),
    'red':       HexColor('#B91C1C'),
    'purple':    HexColor('#6D28D9'),
    'slate':     HexColor('#334155'),
    'muted':     HexColor('#64748B'),
    'light':     HexColor('#94A3B8'),
    'border':    HexColor('#CBD5E1'),
    'bg':        HexColor('#F0F7FF'),
    'bgCard':    HexColor('#F8FAFF'),
    'bgDark':    HexColor('#0D1117'),
    'codeBlue':  HexColor('#7DD3FC'),
    'codeGreen': HexColor('#86EFAC'),
    'codePurp':  HexColor('#C4B5FD'),
    'white':     white,
}

MAX_LINES = 42  # safe lines per code block before splitting

# When True, omit code blocks from the generated PDF (content/process only)
CONTENT_ONLY = True

# ── Custom Flowables ──────────────────────────────────────────────────────────
class Divider(Flowable):
    def __init__(self, color=None, thickness=1, width=None):
        self.div_color = color or P['border']
        self.thickness = thickness
        self.div_width = width
    def draw(self):
        w = self.div_width or CW
        self.canv.setStrokeColor(self.div_color)
        self.canv.setLineWidth(self.thickness)
        self.canv.line(0, 0, w, 0)
    def wrap(self, aw, ah): return (self.div_width or aw, self.thickness + 2)

class ChapterBanner(Flowable):
    def __init__(self, number, title, subtitle='', width=CW):
        self.number = number
        self.title = title
        self.subtitle = subtitle
        self.bw = width
        self.bh = 52
    def draw(self):
        c = self.canv
        # Deep navy bg
        c.setFillColor(P['navy'])
        c.roundRect(0, 0, self.bw, self.bh, 6, stroke=0, fill=1)
        # Left accent
        c.setFillColor(P['accent'])
        c.rect(0, 0, 5, self.bh, stroke=0, fill=1)
        # Chapter number circle
        c.setFillColor(P['accent'])
        c.circle(28, self.bh / 2, 14, stroke=0, fill=1)
        c.setFillColor(white)
        c.setFont('Helvetica-Bold', 13)
        cx = 28 - c.stringWidth(self.number, 'Helvetica-Bold', 13) / 2
        c.drawString(cx, self.bh / 2 - 5, self.number)
        # Title
        c.setFillColor(white)
        c.setFont('Helvetica-Bold', 14)
        c.drawString(52, self.bh / 2 + 4, self.title)
        # Subtitle
        if self.subtitle:
            c.setFillColor(P['sky'])
            c.setFont('Helvetica', 8)
            c.drawString(52, self.bh / 2 - 10, self.subtitle)
    def wrap(self, aw, ah): return (self.bw, self.bh)

class SectionLabel(Flowable):
    def __init__(self, text, color=None, width=CW):
        self.text = text
        self.lcolor = color or P['accent']
        self.lw = width
    def draw(self):
        c = self.canv
        c.setFillColor(self.lcolor)
        c.rect(0, 2, 3, 14, stroke=0, fill=1)
        c.setFillColor(P['slate'])
        c.setFont('Helvetica-Bold', 10)
        c.drawString(10, 4, self.text.upper())
    def wrap(self, aw, ah): return (self.lw, 20)

class CalloutBox(Flowable):
    def __init__(self, icon, label, text, color=None, width=CW):
        self.icon = icon
        self.label = label
        self.text = text
        self.bcolor = color or P['accent']
        self.bw = width
        # Estimate height
        chars_per_line = int((self.bw - 90) / 5.5)
        lines = max(1, len(text) // chars_per_line + 1)
        self.bh = max(44, 24 + lines * 13)
    def draw(self):
        c = self.canv
        c.setFillColor(HexColor('#F0F7FF'))
        c.roundRect(0, 0, self.bw, self.bh, 5, stroke=0, fill=1)
        c.setStrokeColor(self.bcolor)
        c.setLineWidth(1.5)
        c.roundRect(0, 0, self.bw, self.bh, 5, stroke=1, fill=0)
        c.setFillColor(self.bcolor)
        c.rect(0, 0, 4, self.bh, stroke=0, fill=1)
        c.setFont('Helvetica-Bold', 9)
        c.setFillColor(self.bcolor)
        c.drawString(12, self.bh - 14, f'{self.icon}  {self.label}')
        c.setFillColor(P['slate'])
        c.setFont('Helvetica', 8.5)
        # Wrap text manually
        words = self.text.split()
        line, y = '', self.bh - 27
        max_w = self.bw - 22
        for word in words:
            test = (line + ' ' + word).strip()
            if c.stringWidth(test, 'Helvetica', 8.5) < max_w:
                line = test
            else:
                c.drawString(12, y, line)
                line, y = word, y - 13
        if line:
            c.drawString(12, y, line)
    def wrap(self, aw, ah): return (self.bw, self.bh + 4)

class FlowDiagram(Flowable):
    """Renders a vertical flow of labeled steps with arrows."""
    def __init__(self, steps, title='', width=CW):
        self.steps = steps  # list of (label, description, color)
        self.title = title
        self.fw = width
        self.step_h = 28
        self.gap = 14
        self.fh = (self.step_h + self.gap) * len(steps) + (30 if title else 0)
    def draw(self):
        c = self.canv
        y = self.fh
        if self.title:
            c.setFont('Helvetica-Bold', 9)
            c.setFillColor(P['muted'])
            c.drawString(0, y - 12, self.title.upper())
            y -= 28
        for i, (label, desc, color) in enumerate(self.steps):
            step_y = y - self.step_h
            # Box
            c.setFillColor(color)
            c.roundRect(0, step_y, self.fw, self.step_h, 4, stroke=0, fill=1)
            # Label
            c.setFillColor(white)
            c.setFont('Helvetica-Bold', 9)
            c.drawString(10, step_y + 16, label)
            # Desc
            c.setFont('Helvetica', 8)
            c.drawString(10, step_y + 5, desc[:95])
            # Arrow
            if i < len(self.steps) - 1:
                ax = self.fw / 2
                c.setFillColor(P['muted'])
                c.setStrokeColor(P['border'])
                c.setLineWidth(1)
                c.line(ax, step_y - 1, ax, step_y - self.gap + 2)
                # Arrow head
                c.setFillColor(P['muted'])
                p = c.beginPath()
                p.moveTo(ax - 4, step_y - self.gap + 5)
                p.lineTo(ax + 4, step_y - self.gap + 5)
                p.lineTo(ax,     step_y - self.gap)
                p.close()
                c.setFillColor(P['muted'])
                c.drawPath(p, stroke=0, fill=1)
            y -= self.step_h + self.gap
    def wrap(self, aw, ah): return (self.fw, self.fh)

class LayerDiagram(Flowable):
    """Architecture layer stack."""
    def __init__(self, layers, width=CW):
        self.layers = layers  # list of (name, items_str, color)
        self.lw = width
        self.layer_h = 38
        self.lh = self.layer_h * len(layers) + 4
    def draw(self):
        c = self.canv
        for i, (name, items, color) in enumerate(reversed(self.layers)):
            y = i * self.layer_h
            c.setFillColor(color)
            c.roundRect(0, y, self.lw, self.layer_h - 2, 3, stroke=0, fill=1)
            c.setFillColor(white)
            c.setFont('Helvetica-Bold', 9)
            c.drawString(10, y + 22, name)
            c.setFont('Helvetica', 7.5)
            c.setFillColor(HexColor('#DBEAFE'))
            c.drawString(10, y + 9, items)
    def wrap(self, aw, ah): return (self.lw, self.lh)

class CodeBlock(Flowable):
    def __init__(self, lines, lang='', width=CW):
        if isinstance(lines, str):
            lines = lines.strip().split('\n')
        self.lines = lines
        self.lang = lang
        self.cw = width
        self.lh = 11
        self.pad = 8
    def _total_h(self, n=None):
        n = n or len(self.lines)
        return n * self.lh + 2 * self.pad
    def draw(self):
        c = self.canv
        h = self._total_h()
        c.setFillColor(P['bgDark'])
        c.roundRect(0, 0, self.cw, h, 4, stroke=0, fill=1)
        if self.lang:
            lw = c.stringWidth(self.lang, 'Helvetica-Bold', 6.5) + 10
            c.setFillColor(P['blue'])
            c.roundRect(self.cw - lw - 4, h - 15, lw, 12, 2, stroke=0, fill=1)
            c.setFillColor(white)
            c.setFont('Helvetica-Bold', 6.5)
            c.drawString(self.cw - lw, h - 10, self.lang)
        y = h - self.pad - self.lh + 2
        for line in self.lines:
            s = line.rstrip()
            stripped = s.lstrip()
            if stripped.startswith(('//','#','--','/*')):
                c.setFillColor(P['muted'])
            elif stripped.startswith(('public ','private ','protected ','internal ','static ','async ','override ')):
                c.setFillColor(P['codePurp'])
            elif stripped.startswith(('SELECT','INSERT','UPDATE','DELETE','CREATE','ALTER','FROM','WHERE','JOIN')):
                c.setFillColor(P['sky'])
            elif stripped.startswith(('"','\'','[')) or (stripped.startswith('{') and ':' in stripped):
                c.setFillColor(P['codeGreen'])
            else:
                c.setFillColor(P['codeBlue'])
            c.setFont('Courier', 7.5)
            c.drawString(self.pad, y, s[:118])
            y -= self.lh
    def wrap(self, aw, ah): return (self.cw, self._total_h())
    def split(self, aw, ah):
        fit = int((ah - 2 * self.pad) / self.lh) - 2
        if fit >= len(self.lines) or fit <= 0:
            return [self] if self._total_h() <= ah else []
        p1 = CodeBlock(self.lines[:fit], self.lang, self.cw)
        p2 = CodeBlock(self.lines[fit:], '', self.cw)
        return [p1, p2]

# ── Styles ─────────────────────────────────────────────────────────────────────
def S(name, **kw):
    defaults = dict(fontName='Helvetica', textColor=P['ink'], leading=14)
    defaults.update(kw)
    return ParagraphStyle(name, **defaults)

ST = {
    'h1':       S('h1', fontSize=20, fontName='Helvetica-Bold', textColor=P['blue'],
                  spaceBefore=16, spaceAfter=8, leading=24),
    'h2':       S('h2', fontSize=13, fontName='Helvetica-Bold', textColor=P['accent'],
                  spaceBefore=12, spaceAfter=5, leading=16),
    'h3':       S('h3', fontSize=10.5, fontName='Helvetica-Bold', textColor=P['slate'],
                  spaceBefore=9, spaceAfter=4, leading=13),
    'body':     S('body', fontSize=9.5, leading=15, spaceAfter=6,
                  textColor=P['ink'], alignment=TA_JUSTIFY),
    'body_l':   S('body_l', fontSize=9.5, leading=15, spaceAfter=5, textColor=P['ink']),
    'small':    S('small', fontSize=8.5, leading=12, spaceAfter=4, textColor=P['slate']),
    'bullet':   S('bullet', fontSize=9, leading=13, spaceAfter=3,
                  textColor=P['ink'], leftIndent=16, firstLineIndent=-10),
    'sub_bul':  S('sub_bul', fontSize=8.5, leading=12, spaceAfter=2,
                  textColor=P['slate'], leftIndent=30, firstLineIndent=-10),
    'mono':     S('mono', fontName='Courier', fontSize=8.5, leading=12,
                  textColor=P['accent'], backColor=P['bg'], spaceAfter=3),
    'caption':  S('caption', fontSize=8, leading=11, textColor=P['muted'],
                  alignment=TA_CENTER, spaceAfter=6, fontName='Helvetica-Oblique'),
    'cover_t':  S('cover_t', fontSize=36, fontName='Helvetica-Bold',
                  textColor=P['white'], alignment=TA_CENTER, leading=42),
    'cover_s':  S('cover_s', fontSize=15, textColor=HexColor('#93C5FD'),
                  alignment=TA_CENTER, leading=20, spaceAfter=6),
    'cover_v':  S('cover_v', fontSize=9, textColor=HexColor('#64748B'),
                  alignment=TA_CENTER, leading=13),
    'toc_1':    S('toc_1', fontSize=10, fontName='Helvetica-Bold',
                  textColor=P['blue'], spaceAfter=4, leading=13),
    'toc_2':    S('toc_2', fontSize=9, textColor=P['slate'],
                  spaceAfter=2, leading=12, leftIndent=14),
    'tag':      S('tag', fontSize=8, fontName='Helvetica-Bold',
                  textColor=P['accent'], backColor=P['bg'], leading=11),
}

# ── Helpers ───────────────────────────────────────────────────────────────────
def sp(n=6): return Spacer(1, n)
def pb(): return PageBreak()
def hr(c=None, t=0.6): return HRFlowable(width='100%', thickness=t, color=c or P['border'], spaceBefore=4, spaceAfter=4)

def p(text, style='body', **kw):
    return Paragraph(text, ST.get(style, ST['body']))

def bul(text, level=0):
    style = ST['sub_bul'] if level > 0 else ST['bullet']
    sym = '&#9702;' if level > 0 else '&#9658;'
    col = P['muted'].hexval() if level > 0 else P['accent'].hexval()
    return Paragraph(f'<font color="#{col}">{sym}</font>  {text}', style)

def code(lines, lang=''):
    """Returns list of CodeBlock flowables, auto-split if needed."""
    # If content-only mode is enabled, omit code blocks entirely.
    if CONTENT_ONLY:
        return []

    if isinstance(lines, str):
        lines = lines.strip().split('\n')
    blocks = []
    for i in range(0, len(lines), MAX_LINES):
        chunk = lines[i:i+MAX_LINES]
        blocks.append(CodeBlock(chunk, lang=lang if i==0 else '', width=CW))
        if i + MAX_LINES < len(lines):
            blocks.append(sp(3))
    return blocks

def tbl(headers, rows, widths, stripe=True):
    data = [headers] + rows
    style = TableStyle([
        ('BACKGROUND',    (0,0), (-1,0),  P['blue']),
        ('TEXTCOLOR',     (0,0), (-1,0),  white),
        ('FONTNAME',      (0,0), (-1,0),  'Helvetica-Bold'),
        ('FONTSIZE',      (0,0), (-1,0),  8),
        ('TOPPADDING',    (0,0), (-1,-1), 5),
        ('BOTTOMPADDING', (0,0), (-1,-1), 5),
        ('LEFTPADDING',   (0,0), (-1,-1), 7),
        ('RIGHTPADDING',  (0,0), (-1,-1), 7),
        ('FONTNAME',      (0,1), (-1,-1), 'Helvetica'),
        ('FONTSIZE',      (0,1), (-1,-1), 8),
        ('TEXTCOLOR',     (0,1), (-1,-1), P['ink']),
        ('GRID',          (0,0), (-1,-1), 0.4, P['border']),
        ('VALIGN',        (0,0), (-1,-1), 'MIDDLE'),
        ('ROWBACKGROUNDS',(0,1), (-1,-1), [white, P['bgCard']]),
    ])
    t = Table(data, colWidths=widths, repeatRows=1)
    t.setStyle(style)
    return t

def info(icon, label, text, color=None):
    return CalloutBox(icon, label, text, color or P['accent'], CW)

# ── Page callbacks ────────────────────────────────────────────────────────────
def draw_page(canvas, doc):
    canvas.saveState()
    # Header
    canvas.setFillColor(P['navy'])
    canvas.rect(0, H - 14*mm, W, 14*mm, stroke=0, fill=1)
    canvas.setFillColor(P['accent'])
    canvas.rect(0, H - 14*mm, 4, 14*mm, stroke=0, fill=1)
    canvas.setFillColor(white)
    canvas.setFont('Helvetica-Bold', 8)
    canvas.drawString(MARGIN, H - 9*mm, 'DYNAMIC DATA ENGINE')
    canvas.setFont('Helvetica', 7.5)
    canvas.setFillColor(HexColor('#7DD3FC'))
    canvas.drawRightString(W - MARGIN, H - 9*mm, 'Architecture & Developer Guide  ·  v2.0')
    # Footer
    canvas.setStrokeColor(P['border'])
    canvas.setLineWidth(0.5)
    canvas.line(MARGIN, 10*mm, W - MARGIN, 10*mm)
    canvas.setFillColor(P['light'])
    canvas.setFont('Helvetica', 7)
    canvas.drawString(MARGIN, 6*mm, 'Confidential — Internal Engineering Reference')
    canvas.setFont('Helvetica-Bold', 8)
    canvas.setFillColor(P['slate'])
    canvas.drawRightString(W - MARGIN, 6*mm, f'{doc.page}')
    canvas.restoreState()

# ══════════════════════════════════════════════════════════════════════════════
# STORY
# ══════════════════════════════════════════════════════════════════════════════
story = []

# ─────────────────────────────────────────────────────────────────────────────
# COVER
# ─────────────────────────────────────────────────────────────────────────────
def cover_page(c, doc):
    c.saveState()
    # Full bleed gradient background
    c.setFillColor(P['navy'])
    c.rect(0, 0, W, H, stroke=0, fill=1)
    # Geometric accent shapes
    c.setFillColor(P['accent'])
    c.setStrokeColor(HexColor('#1D4ED8'))
    c.setLineWidth(0)
    c.rect(0, 0, W, 3, stroke=0, fill=1)          # bottom stripe
    c.rect(0, H-3, W, 3, stroke=0, fill=1)         # top stripe
    c.setFillColor(HexColor('#1E3A8A'))
    c.roundRect(MARGIN, 80*mm, CW, 110*mm, 8, stroke=0, fill=1)
    # Accent line
    c.setFillColor(P['accent'])
    c.rect(MARGIN, 185*mm, 50*mm, 3, stroke=0, fill=1)
    # Product badge
    c.setFillColor(P['accent'])
    c.roundRect(MARGIN, 195*mm, 55*mm, 16, 4, stroke=0, fill=1)
    c.setFillColor(white)
    c.setFont('Helvetica-Bold', 8)
    c.drawString(MARGIN + 6, 199*mm, 'DEVELOPER REFERENCE  v2.0')
    # Title
    c.setFillColor(white)
    c.setFont('Helvetica-Bold', 34)
    c.drawString(MARGIN, 165*mm, 'Dynamic Data Engine')
    c.setFont('Helvetica-Bold', 22)
    c.setFillColor(HexColor('#93C5FD'))
    c.drawString(MARGIN, 152*mm, 'Architecture & Implementation Guide')
    # Subtitle
    c.setFont('Helvetica', 11)
    c.setFillColor(HexColor('#94A3B8'))
    c.drawString(MARGIN, 140*mm, 'Metadata-Driven CRUD Engine for Multi-Database Enterprise Systems')
    # Description box
    desc = (
        'A comprehensive reference covering TransactionService, FieldMapper, '
        'ValidationService, AutoNumberService, DataTypeConverter, OperationScope, '
        'multi-provider support (PostgreSQL / MySQL / SQL Server / Oracle), '
        'audit logging, child table persistence, and React frontend integration.'
    )
    c.setFont('Helvetica', 9)
    c.setFillColor(HexColor('#CBD5E1'))
    tw = CW - 10
    words = desc.split()
    line, y = '', 124*mm
    for word in words:
        test = (line + ' ' + word).strip()
        if c.stringWidth(test, 'Helvetica', 9) < tw:
            line = test
        else:
            c.drawString(MARGIN, y, line)
            line, y = word, y - 13
    if line:
        c.drawString(MARGIN, y, line)
    # Meta table
    meta = [
        ('Document Type', 'Architecture + Implementation Reference'),
        ('Version',       '2.0  —  Enhanced Edition'),
        ('Classification','Internal Engineering  ·  Confidential'),
        ('Scope',         'Backend .NET 8 DLL + React 18 Frontend Suite'),
        ('Providers',     'PostgreSQL  ·  MySQL  ·  SQL Server  ·  Oracle'),
    ]
    c.setFont('Helvetica-Bold', 8)
    c.setFillColor(HexColor('#3B82F6'))
    by = 90*mm
    for k, v in meta:
        c.setFillColor(HexColor('#1E40AF'))
        c.rect(MARGIN, by - 1, CW, 14, stroke=0, fill=1)
        c.setFillColor(HexColor('#93C5FD'))
        c.setFont('Helvetica-Bold', 8)
        c.drawString(MARGIN + 6, by + 3, k)
        c.setFillColor(white)
        c.setFont('Helvetica', 8)
        c.drawString(MARGIN + 60*mm, by + 3, v)
        by -= 16
    # Footer band
    c.setFillColor(HexColor('#0A0F1E'))
    c.rect(0, 0, W, 20*mm, stroke=0, fill=1)
    c.setFillColor(HexColor('#334155'))
    c.setFont('Helvetica', 8)
    c.drawString(MARGIN, 8*mm, 'Internal Engineering  ·  Confidential')
    c.drawRightString(W - MARGIN, 8*mm, 'Dynamic Data Engine — All Rights Reserved')
    c.restoreState()

# Blank first page for cover
story.append(Paragraph('', ParagraphStyle('blank', fontSize=1)))
story.append(PageBreak())

# ─────────────────────────────────────────────────────────────────────────────
# TABLE OF CONTENTS
# ─────────────────────────────────────────────────────────────────────────────
story += [
    p('Table of Contents', 'h1'),
    hr(P['accent'], 1.5), sp(6),
]
toc_entries = [
    ('1', 'Executive Summary & Design Philosophy', '3'),
    ('2', 'Architecture Overview', '4'),
    ('2.1', 'Component Layer Diagram', '4'),
    ('2.2', 'Multi-Database Provider Architecture', '5'),
    ('3', 'Core Data Models & Contracts', '6'),
    ('3.1', 'Transaction Payload (rootEntity / forge)', '6'),
    ('3.2', 'Fetch Payload & Query Model', '7'),
    ('3.3', 'FieldMapper Metadata Model', '7'),
    ('4', 'Registry & Metadata System', '8'),
    ('4.1', 'EntityRegistry & ColumnRegistry', '8'),
    ('4.2', 'DbProfiles & ConnectionFactory', '9'),
    ('5', 'TransactionService — CRUD Orchestrator', '10'),
    ('5.1', 'Insert Flow', '10'),
    ('5.2', 'Update Flow', '11'),
    ('5.3', 'Delete (Soft) Flow', '12'),
    ('5.4', 'Child Table Persistence', '12'),
    ('6', 'FieldMapper & Dynamic SQL Builder', '13'),
    ('6.1', 'FetchSqlBuilder', '14'),
    ('6.2', 'ForgeSqlBuilder', '15'),
    ('6.3', 'Dialect Abstraction (Multi-DB)', '16'),
    ('7', 'ValidationService', '17'),
    ('8', 'AutoNumberService', '18'),
    ('9', 'DataTypeConverter', '19'),
    ('10', 'OperationScope & Transaction Safety', '20'),
    ('11', 'AuditService', '21'),
    ('12', 'ConnectionFactory & Pool Management', '22'),
    ('13', 'WhitelistValidator & Security', '23'),
    ('14', 'FetchService — Read Engine', '24'),
    ('15', 'API Endpoint Design', '25'),
    ('16', 'React Frontend Suite', '26'),
    ('17', 'Error Handling Strategy', '28'),
    ('18', 'Testing Strategy', '29'),
    ('19', 'Enhancement Roadmap', '30'),
]
for num, title, pg in toc_entries:
    indent = 14 if '.' in num else 0
    style = ST['toc_2'] if '.' in num else ST['toc_1']
    dots = '.' * max(2, 80 - len(num+title))
    story.append(Paragraph(
        f'<font color="#{P["accent"].hexval()}">{num}</font>  {title}'
        f'<font color="#{P["light"].hexval()}">  {dots}  {pg}</font>',
        style
    ))
story.append(pb())

# ─────────────────────────────────────────────────────────────────────────────
# CHAPTER 1 — EXECUTIVE SUMMARY
# ─────────────────────────────────────────────────────────────────────────────
story += [
    ChapterBanner('1', 'Executive Summary & Design Philosophy',
                  'What this engine is and why it exists'),
    sp(10),
    p('This document describes a <b>metadata-driven, multi-provider CRUD engine</b> '
      'designed to eliminate per-table repository boilerplate in enterprise applications. '
      'Rather than writing a Service, Repository, and DTO for every database table, '
      'this engine accepts a structured JSON request, resolves the target table from a '
      'central registry, and dynamically generates fully parameterised SQL at runtime.',
      'body'),
    p('The engine is built on four foundational pillars drawn from production enterprise '
      'data platforms and enhanced with modern .NET 8 patterns:', 'body'),
    sp(4),
]
pillars = [
    ('Metadata-Driven', 'FieldMapper metadata governs every table\'s columns, types, PK, required flags, and child relationships — no hardcoded schema anywhere in the engine.', P['accent']),
    ('Multi-Provider', 'A SqlDialect abstraction layer covers PostgreSQL, MySQL, SQL Server, and Oracle — parameter syntax, pagination, and returning-clause differences are encapsulated per dialect.', P['teal']),
    ('Transactional', 'OperationScope wraps every write in a database transaction. Parent inserts, all child inserts, and audit writes commit atomically or roll back together.', P['purple']),
    ('Observable', 'AuditService captures before/after snapshots on every update and delete. AutoNumberService generates domain-specific keys. ValidationService enforces rules before any SQL is constructed.', P['green']),
]
for name, desc, color in pillars:
    story.append(KeepTogether([
        SectionLabel(name, color),
        sp(3),
        p(desc, 'body_l'),
        sp(4),
    ]))

story += [
    hr(P['border']),
    p('<b>Core Responsibility in One Sentence:</b> Accept a structured transaction request, '
      'resolve all metadata from the registry, validate the payload, generate safe parameterised SQL, '
      'execute atomically with parent-child support, capture an audit trail, and return a typed result.',
      'body'),
    sp(4),
    p('Design Philosophy', 'h2'),
    hr(P['sky'], 0.8),
    tbl(
        ['Principle', 'Implementation Decision'],
        [
            ['Generic over specific', 'One TransactionService handles any registered table — no table-specific code'],
            ['Registry as truth', 'FieldMapper metadata in the database is the single source for all schema knowledge'],
            ['Safe by default', 'All values are Dapper parameters; column/table names validated against registry whitelist'],
            ['Atomic always', 'OperationScope ensures parent + children commit or rollback as one unit'],
            ['Dialect-agnostic', 'SqlDialect value type isolates all provider differences from business logic'],
            ['Auditable', 'Every update and delete captures old/new state before commit'],
            ['Observable', 'Serilog structured events on every operation with entity, provider, duration'],
        ],
        [65*mm, CW - 65*mm]
    ),
    pb(),
]

# ─────────────────────────────────────────────────────────────────────────────
# CHAPTER 2 — ARCHITECTURE OVERVIEW
# ─────────────────────────────────────────────────────────────────────────────
story += [
    ChapterBanner('2', 'Architecture Overview', 'Component layers and system map'),
    sp(10),
    p('The engine is structured into five distinct layers, each with a single '
      'responsibility. No layer reaches past its immediate neighbour.', 'body'),
    sp(8),
    LayerDiagram([
        ('DATABASE TIER',        'PostgreSQL  ·  MySQL  ·  SQL Server  ·  Oracle  ·  SQLite (dev)', HexColor('#0F172A')),
        ('PROVIDER LAYER',       'EnhancedPostgreSqlProvider  ·  MySqlProvider  ·  SqlServerProvider  ·  OracleProvider', HexColor('#1E3A8A')),
        ('INFRASTRUCTURE',       'DatabaseConnectionFactory  ·  EnhancedConnectionFactory  ·  SqlDialect  ·  OperationScope', HexColor('#1D4ED8')),
        ('SERVICE CORE',         'TransactionService  ·  FetchService  ·  ValidationService  ·  AutoNumberService  ·  AuditService', HexColor('#2563EB')),
        ('METADATA / REGISTRY',  'FieldMapperService  ·  EntityRegistryService  ·  ColumnRegistryService  ·  DataTypeConverter', HexColor('#3B82F6')),
        ('API / CONSUMER LAYER', 'DataController (/forge)  ·  FetchController (/fetch)  ·  RegistryController  ·  React Frontend', HexColor('#60A5FA')),
    ]),
    sp(6),
    p('Figure 1 — Architecture layer stack (bottom = database, top = consumer)', 'caption'),
    sp(8),
    p('2.1  Full System Component Map', 'h2'),
    hr(P['sky'], 0.8),
] + code([
    '┌──────────────────────────────────────────────────────────────────────────────┐',
    '│                          CONSUMER LAYER                                      │',
    '│  React: QueryBuilder · FieldMapper · RegistryManager                        │',
    '│  POST /forge   POST /fetch   GET|POST /registry                              │',
    '└──────────────────────┬───────────────────────┬──────────────────────────────┘',
    '                       │                        │',
    '┌──────────────────────▼────────────────────────▼──────────────────────────────┐',
    '│                        SERVICE CORE                                           │',
    '│                                                                               │',
    '│   TransactionService          FetchService         RegistryController        │',
    '│        │                           │                      │                   │',
    '│        ▼                           ▼                      ▼                   │',
    '│   ValidationService         FetchSqlBuilder       FieldMapperService         │',
    '│   AutoNumberService         WhitelistValidator    EntityRegistryService      │',
    '│   AuditService              DataTypeConverter     ColumnRegistryService      │',
    '│   OperationScope                                                              │',
    '│        │                           │                                          │',
    '│        └──────────────┬────────────┘                                          │',
    '│                       ▼                                                       │',
    '│              ForgeSqlBuilder / FetchSqlBuilder  ← SqlDialect                 │',
    '└───────────────────────┬───────────────────────────────────────────────────────┘',
    '                        │ IEnhancedDataProvider',
    '┌───────────────────────▼───────────────────────────────────────────────────────┐',
    '│                       PROVIDER LAYER                                          │',
    '│   EnhancedPostgreSqlProvider  ·  MySqlProvider                               │',
    '│   SqlServerProvider           ·  OracleProvider                              │',
    '└───────────────────────┬───────────────────────────────────────────────────────┘',
    '                        │ IDbConnection',
    '┌───────────────────────▼───────────────────────────────────────────────────────┐',
    '│  DatabaseConnectionFactory  /  EnhancedConnectionFactory                     │',
    '│  ┌────────────────┐  ┌─────────────┐  ┌────────────────┐  ┌───────────────┐ │',
    '│  │ PostgreSQL ERP │  │ MySQL HR    │  │ SQL Server RPT │  │ Oracle Legacy │ │',
    '│  └────────────────┘  └─────────────┘  └────────────────┘  └───────────────┘ │',
    '└───────────────────────────────────────────────────────────────────────────────┘',
], lang='SYSTEM MAP') + [
    sp(4),
    p('2.2  Multi-Database Provider Architecture', 'h2'),
    hr(P['sky'], 0.8),
    p('Every provider implements <b>IEnhancedDataProvider</b>, a contract that abstracts '
      'all database execution. The SqlDialect value type carries provider-specific SQL '
      'syntax differences so that TransactionService and FetchService never branch on provider type.', 'body'),
    sp(4),
    tbl(
        ['Dialect Concern', 'PostgreSQL', 'MySQL', 'SQL Server', 'Oracle'],
        [
            ['Param prefix',    '@col',               '@col',               '@col',                     ':col'],
            ['Pagination',      'LIMIT n OFFSET s',   'LIMIT n OFFSET s',   'OFFSET s ROWS FETCH NEXT n', 'OFFSET s ROWS FETCH NEXT n'],
            ['Returning new ID','RETURNING id',       'SELECT LAST_INSERT_ID()', 'SELECT SCOPE_IDENTITY()', 'RETURNING id INTO :newId'],
            ['Schema separator','"schema"."table"',   '`schema`.`table`',   '[schema].[table]',         'SCHEMA.TABLE'],
            ['Bool type',       'BOOLEAN',            'TINYINT(1)',          'BIT',                      'NUMBER(1)'],
            ['UUID type',       'UUID',               'CHAR(36)',            'UNIQUEIDENTIFIER',         'VARCHAR2(36)'],
            ['Current time',    'NOW()',               'NOW()',               'GETUTCDATE()',              'SYSDATE'],
        ],
        [38*mm, 34*mm, 34*mm, 37*mm, CW - 143*mm]
    ),
    pb(),
]

# ─────────────────────────────────────────────────────────────────────────────
# CHAPTER 3 — CORE DATA MODELS
# ─────────────────────────────────────────────────────────────────────────────
story += [
    ChapterBanner('3', 'Core Data Models & Contracts',
                  'Request/response shapes and FieldMapper metadata'),
    sp(10),
    p('3.1  Transaction Payload — /forge (Write Operations)', 'h2'),
    hr(P['sky'], 0.8),
    p('The forge payload is the universal write contract. It handles create, update, and '
      'soft-delete for any registered entity, including nested child records in one atomic call.', 'body'),
] + code([
    '// ForgeRequest.cs — Universal write contract',
    'public sealed record ForgeRequest',
    '{',
    '    public string  RootEntity { get; init; }            // registered table name',
    '    public string? RootId     { get; init; }            // null=CREATE, value=UPDATE/DELETE',
    '    public string  Operation  { get; init; }            // "create" | "update" | "delete"',
    '    public Dictionary<string, object?> Content { get; init; } = new();',
    '    public List<NodeForge>? Nodes { get; init; }        // child table records',
    '}',
    '',
    'public sealed record NodeForge',
    '{',
    '    public string  NodeEntity  { get; init; }           // child table name',
    '    public string  ParentLink  { get; init; }           // FK column on child',
    '    public string? NodeId      { get; init; }           // null=INSERT child, value=UPDATE',
    '    public Dictionary<string, object?> Content { get; init; } = new();',
    '}',
    '',
    '// ForgeResult.cs',
    'public sealed record ForgeResult',
    '{',
    '    public bool    Success      { get; init; }',
    '    public string? EntityId     { get; init; }          // new/existing PK value',
    '    public string  Operation    { get; init; }',
    '    public int     AffectedNodes { get; init; }         // child rows affected',
    '    public string? Error        { get; init; }',
    '    public ForgeMeta Meta       { get; init; }          // entity, provider, ms',
    '}',
], lang='C#') + [
    sp(6),
    p('3.2  Fetch Payload — /fetch (Read Operations)', 'h2'),
    hr(P['sky'], 0.8),
] + code([
    '// FetchRequest.cs — Universal read contract',
    'public sealed record FetchRequest',
    '{',
    '    public string        RootEntity   { get; init; }    // registered table name',
    '    public int           Page         { get; init; } = 1;',
    '    public int           PageSize     { get; init; } = 25;     // max 1000',
    '    public bool          IncludeCount { get; init; } = true;',
    '    public List<string>? Select       { get; init; }    // column projection',
    '    public List<FilterCondition> Filters { get; init; } = new();',
    '    public SearchOptions? Search      { get; init; }    // full-text LIKE',
    '    public SortOptions?   Sort        { get; init; }',
    '    public List<NodeRequest>? Nodes   { get; init; }    // child JOIN nodes',
    '}',
    '',
    'public record FilterCondition(string Column, string Op, object? Value, object? Value2);',
    'public record SearchOptions(string Term, List<string> Columns);',
    'public record SortOptions(string Column, string Direction);  // asc | desc',
    'public record NodeRequest(string NodeEntity, string ParentLink);',
], lang='C#') + [
    sp(6),
    p('3.3  FieldMapper Metadata Model', 'h2'),
    hr(P['sky'], 0.8),
    p('FieldMapper rows are stored in the registry database. They define every aspect '
      'of a table\'s behaviour — the engine reads them at runtime to drive all SQL generation, '
      'validation, type conversion, and child relationship handling.', 'body'),
] + code([
    '// FieldMapperRow.cs — One row per column per table in the registry',
    'public class FieldMapperRow',
    '{',
    '    public string  TableName     { get; set; }    // exact DB table name',
    '    public string  ColumnName    { get; set; }    // exact DB column name',
    '    public string  FieldName     { get; set; }    // UI / API key name',
    '    public string  DataType      { get; set; }    // text|int|decimal|bool|date|uuid',
    '    public bool    IsRequired    { get; set; }',
    '    public bool    IsPrimaryKey  { get; set; }',
    '    public bool    IsReadOnly    { get; set; }    // excluded from INSERT/UPDATE',
    '    public bool    IsAuditField  { get; set; }    // auto-set by engine',
    '    public string? ChildTable    { get; set; }    // null if not a relationship',
    '    public string? ParentLinkCol { get; set; }    // FK column on child table',
    '    public string? LookupTable   { get; set; }    // for lookup/reference fields',
    '    public string? AutoNumberKey { get; set; }    // null if not auto-numbered',
    '    public int     MaxLength     { get; set; }',
    '}',
    '',
    '// Example mapper row for a text column:',
    '// { TableName:"customer", ColumnName:"customer_name",',
    '//   FieldName:"CustomerName", DataType:"text", IsRequired:true }',
], lang='C#') + [pb()]

# ─────────────────────────────────────────────────────────────────────────────
# CHAPTER 4 — REGISTRY & METADATA
# ─────────────────────────────────────────────────────────────────────────────
story += [
    ChapterBanner('4', 'Registry & Metadata System',
                  'EntityRegistry, ColumnRegistry, DbProfiles'),
    sp(10),
    p('4.1  Registry Database Schema', 'h2'),
    hr(P['sky'], 0.8),
    p('The registry is a dedicated metadata database (typically lightweight PostgreSQL or SQLite) '
      'that is entirely separate from any business database. It is the engine\'s single source of '
      'truth for all schema knowledge.', 'body'),
    sp(4),
    p('DbProfiles Table', 'h3'),
    tbl(
        ['Column', 'Type', 'Description'],
        [
            ['profile_id',     'UUID PK',      'Unique profile identifier'],
            ['profile_name',   'VARCHAR(100)', 'Human name e.g. PostgresERP, MySqlHR'],
            ['provider',       'VARCHAR(20)',  'postgresql | mysql | sqlserver | oracle | sqlite'],
            ['host',           'VARCHAR(255)', 'Server hostname or IP'],
            ['port',           'INT',          'Default: PG 5432, MySQL 3306, MSSQL 1433, Oracle 1521'],
            ['database_name',  'VARCHAR(255)', 'DB name or SQLite file path'],
            ['username',       'VARCHAR(100)', 'Connection username'],
            ['password_hash',  'VARCHAR(500)', 'AES-256-CBC encrypted — never plaintext'],
            ['status',         'VARCHAR(20)',  'active | inactive | error'],
            ['created_at',     'TIMESTAMPTZ',  'UTC creation timestamp'],
        ],
        [42*mm, 35*mm, CW - 77*mm]
    ),
    sp(6),
    p('EntityRegistry Table', 'h3'),
    tbl(
        ['Column', 'Type', 'Description'],
        [
            ['entity_id',         'UUID PK',      'Unique entity identifier'],
            ['profile_id',        'UUID FK',       'FK → DbProfiles.profile_id'],
            ['entity_name',       'VARCHAR(200)',  'Exact DB table name (e.g. purchase_order)'],
            ['schema_name',       'VARCHAR(100)',  'Schema prefix (e.g. public, dbo, JANATICS)'],
            ['pk_column',         'VARCHAR(100)',  'Primary key column name (e.g. id)'],
            ['pk_type',           'VARCHAR(20)',   'uuid | autoincrement | autonumber'],
            ['auto_number_key',   'VARCHAR(100)',  'AutoNumber config key if pk_type=autonumber'],
            ['soft_delete_col',   'VARCHAR(100)',  'Column to flag on delete (e.g. is_deleted)'],
            ['allowed_roles',     'VARCHAR(500)',  'Comma-separated roles for access control'],
            ['is_readonly',       'BOOLEAN',       'Block all forge operations'],
            ['created_by',        'VARCHAR(100)',  'Admin who registered this entity'],
        ],
        [42*mm, 35*mm, CW - 77*mm]
    ),
    sp(6),
    p('ColumnRegistry Table', 'h3'),
    tbl(
        ['Column', 'Type', 'Description'],
        [
            ['column_id',    'UUID PK',      'Unique column identifier'],
            ['entity_id',    'UUID FK',       'FK → EntityRegistry.entity_id (CASCADE DELETE)'],
            ['column_name',  'VARCHAR(200)',  'Exact DB column name'],
            ['field_name',   'VARCHAR(200)',  'API/UI field name (used in request Content dict)'],
            ['data_type',    'VARCHAR(50)',   'text | int | decimal | bool | date | uuid | json'],
            ['is_required',  'BOOLEAN',       'Validated on forge create operations'],
            ['is_pk',        'BOOLEAN',       'True if this is the primary key'],
            ['is_readonly',  'BOOLEAN',       'Excluded from INSERT and UPDATE'],
            ['is_audit',     'BOOLEAN',       'Auto-set by engine (created_at, modified_by etc.)'],
            ['is_fk',        'BOOLEAN',       'References another entity'],
            ['fk_reference', 'VARCHAR(300)',  'entity_name.column_name (e.g. vendor.vendor_id)'],
            ['max_length',   'INT',           'Max character length for validation'],
            ['child_entity', 'VARCHAR(200)',  'Child entity name if this column is a relationship'],
            ['parent_link',  'VARCHAR(200)',  'FK column on child table pointing back to parent'],
        ],
        [42*mm, 35*mm, CW - 77*mm]
    ),
    sp(6),
    p('4.2  EntityRegistryService — Cache & Lookup', 'h2'),
    hr(P['sky'], 0.8),
    p('The registry service wraps all MetaDB queries with an <b>IMemoryCache</b> layer. '
      'Cache keys are namespaced by entity name and profile ID. TTL is configurable '
      '(default 5 minutes). Admin updates via the Registry Manager UI call an invalidation '
      'endpoint that removes affected cache entries immediately.', 'body'),
] + code([
    '// EntityRegistryService.cs — Core lookup with caching',
    'public async Task<EntityMeta> LookupAsync(string entityName)',
    '{',
    '    var cacheKey = $"entity:{entityName.ToLower()}";',
    '    if (_cache.TryGetValue(cacheKey, out EntityMeta cached)) return cached;',
    '',
    '    // Query MetaDB: EntityRegistry JOIN DbProfiles',
    '    var meta = await _metaDb.QuerySingleAsync<EntityMeta>(',
    '        @"SELECT e.*, p.provider, p.password_hash, p.host, p.port, p.database_name',
    '          FROM entity_registry e',
    '          JOIN db_profiles p ON p.profile_id = e.profile_id',
    '          WHERE LOWER(e.entity_name) = LOWER(@name)", new { name = entityName });',
    '',
    '    if (meta is null) throw new EntityNotFoundException(entityName);',
    '',
    '    // Load column metadata',
    '    meta.Columns = (await _metaDb.QueryAsync<ColumnMeta>(',
    '        "SELECT * FROM column_registry WHERE entity_id = @id",',
    '        new { id = meta.EntityId })).ToList();',
    '',
    '    _cache.Set(cacheKey, meta, TimeSpan.FromMinutes(_config.CacheTtlMinutes));',
    '    return meta;',
    '}',
], lang='C#') + [pb()]

# ─────────────────────────────────────────────────────────────────────────────
# CHAPTER 5 — TRANSACTION SERVICE
# ─────────────────────────────────────────────────────────────────────────────
story += [
    ChapterBanner('5', 'TransactionService — CRUD Orchestrator',
                  'The central engine for all create, update, and delete operations'),
    sp(10),
    p('TransactionService is the heart of the engine. It receives a ForgeRequest, '
      'orchestrates all supporting services, and returns a ForgeResult. It never generates '
      'SQL directly — that responsibility belongs to ForgeSqlBuilder. It never validates '
      'directly — that belongs to ValidationService. It is purely an orchestrator.', 'body'),
    sp(6),
    p('5.1  Insert (Create) Flow', 'h2'),
    hr(P['sky'], 0.8),
    sp(4),
    FlowDiagram([
        ('1. Receive ForgeRequest',    'RootEntity, operation="create", Content dict, optional Nodes[]', P['blue']),
        ('2. Registry Lookup',         'EntityRegistryService.LookupAsync → EntityMeta + ColumnMeta[]', HexColor('#1D4ED8')),
        ('3. Whitelist Validate',      'WhitelistValidator: entity registered? columns known? roles allowed?', HexColor('#2563EB')),
        ('4. ValidationService',       'Required fields? Type correctness? Length checks? Business rules?', HexColor('#3B82F6')),
        ('5. DataTypeConverter',       'Convert all Content values to DB-safe .NET types', HexColor('#60A5FA')),
        ('6. AutoNumberService',       'Generate domain key if pk_type=autonumber (e.g. PO-2026-001)', HexColor('#0D9488')),
        ('7. ID Generation',           'GUID.NewGuid() if pk_type=uuid; else use AutoNumber result', HexColor('#0F766E')),
        ('8. AuditService.Stamp',      'Inject created_at=now, created_by=user, is_deleted=false', HexColor('#6D28D9')),
        ('9. Open OperationScope',     'IDbConnection.Open() + BeginTransaction() wrapped in scope', HexColor('#7C3AED')),
        ('10. ForgeSqlBuilder.Build',  'Generate INSERT INTO schema.table (cols) VALUES (@p0,@p1,...)', P['amber']),
        ('11. Execute + Capture ID',   'Provider.ExecuteScalarAsync → RETURNING id / SCOPE_IDENTITY()', HexColor('#B45309')),
        ('12. Process Child Nodes',    'For each NodeForge: inject parentLink FK, Build, Execute', HexColor('#B45309')),
        ('13. Commit + Audit Write',   'OperationScope.Commit() then queue audit_log insert', HexColor('#15803D')),
        ('14. Return ForgeResult',     'success=true, EntityId=newId, AffectedNodes=count', P['green']),
    ], width=CW),
    sp(4),
] + code([
    '// TransactionService.cs — Insert orchestration (simplified)',
    'public async Task<ForgeResult> CreateAsync(ForgeRequest req, ClaimsPrincipal user)',
    '{',
    '    // 1-3: Lookup + validate',
    '    var meta    = await _registry.LookupAsync(req.RootEntity);',
    '    _whitelist.ValidateForge(req, meta, user);',
    '    var errors  = _validation.Validate(req.Content, meta.Columns, "create");',
    '    if (errors.Any()) throw new ValidationException(errors);',
    '',
    '    // 4-6: Convert types, generate ID, stamp audit fields',
    '    var content = _converter.Convert(req.Content, meta.Columns);',
    '    var newId   = meta.PkType == "uuid"',
    '                    ? Guid.NewGuid().ToString()',
    '                    : await _autoNumber.GenerateAsync(meta.AutoNumberKey);',
    '    content[meta.PkColumn]       = newId;',
    '    content["created_at"]        = DateTime.UtcNow;',
    '    content["created_by"]        = user.Identity?.Name;',
    '    content["is_deleted"]        = false;',
    '',
    '    // 7: Build SQL',
    '    var dialect  = SqlDialect.For(meta.Provider);',
    '    var (sql, p) = ForgeSqlBuilder.BuildInsert(meta, content, dialect);',
    '',
    '    // 8: Execute in transaction scope',
    '    await using var scope = await _scopeFactory.CreateAsync(meta.ProfileId);',
    '    await scope.ExecuteAsync(sql, p);',
    '',
    '    // 9: Children',
    '    int childCount = 0;',
    '    foreach (var node in req.Nodes ?? new()) {',
    '        var childMeta     = await _registry.LookupAsync(node.NodeEntity);',
    '        var childContent  = _converter.Convert(node.Content, childMeta.Columns);',
    '        childContent[node.ParentLink] = newId;   // inject FK',
    '        var (cSql, cP)    = ForgeSqlBuilder.BuildInsert(childMeta, childContent, dialect);',
    '        await scope.ExecuteAsync(cSql, cP);',
    '        childCount++;',
    '    }',
    '',
    '    await scope.CommitAsync();',
    '    await _audit.LogCreateAsync(meta.EntityName, newId, content, user);',
    '    return new ForgeResult(true, newId, "create", childCount, null, BuildMeta(meta));',
    '}',
], lang='C#') + [
    sp(6),
    p('5.2  Update Flow', 'h2'),
    hr(P['sky'], 0.8),
] + code([
    'public async Task<ForgeResult> UpdateAsync(ForgeRequest req, ClaimsPrincipal user)',
    '{',
    '    var meta    = await _registry.LookupAsync(req.RootEntity);',
    '    _whitelist.ValidateForge(req, meta, user);',
    '',
    '    // Capture existing state for audit BEFORE any changes',
    '    var existing = await _fetch.GetByIdAsync(req.RootEntity, req.RootId!);',
    '    if (existing is null) throw new EntityNotFoundException(req.RootEntity, req.RootId!);',
    '',
    '    var errors = _validation.Validate(req.Content, meta.Columns, "update");',
    '    if (errors.Any()) throw new ValidationException(errors);',
    '',
    '    // Convert, stamp modified fields, exclude readonly cols',
    '    var content = _converter.Convert(req.Content, meta.Columns);',
    '    content["modified_at"] = DateTime.UtcNow;',
    '    content["modified_by"] = user.Identity?.Name;',
    '    var updateContent = content',
    '        .Where(kv => !meta.Columns.First(c => c.FieldName == kv.Key).IsReadOnly)',
    '        .ToDictionary(kv => kv.Key, kv => kv.Value);',
    '',
    '    var dialect  = SqlDialect.For(meta.Provider);',
    '    var (sql, p) = ForgeSqlBuilder.BuildUpdate(meta, req.RootId!, updateContent, dialect);',
    '',
    '    await using var scope = await _scopeFactory.CreateAsync(meta.ProfileId);',
    '    await scope.ExecuteAsync(sql, p);',
    '',
    '    // Child nodes: INSERT new / UPDATE existing based on NodeId',
    '    int childCount = 0;',
    '    foreach (var node in req.Nodes ?? new()) {',
    '        var childMeta = await _registry.LookupAsync(node.NodeEntity);',
    '        var childContent = _converter.Convert(node.Content, childMeta.Columns);',
    '        childContent[node.ParentLink] = req.RootId!;',
    '        if (node.NodeId is null) {',
    '            var (cSql, cP) = ForgeSqlBuilder.BuildInsert(childMeta, childContent, dialect);',
    '            await scope.ExecuteAsync(cSql, cP);',
    '        } else {',
    '            var (cSql, cP) = ForgeSqlBuilder.BuildUpdate(childMeta, node.NodeId, childContent, dialect);',
    '            await scope.ExecuteAsync(cSql, cP);',
    '        }',
    '        childCount++;',
    '    }',
    '',
    '    await scope.CommitAsync();',
    '    await _audit.LogUpdateAsync(meta.EntityName, req.RootId!, existing, content, user);',
    '    return new ForgeResult(true, req.RootId!, "update", childCount, null, BuildMeta(meta));',
    '}',
], lang='C#') + [pb()]

# ─────────────────────────────────────────────────────────────────────────────
# CHAPTER 6 — FIELD MAPPER & SQL BUILDER
# ─────────────────────────────────────────────────────────────────────────────
story += [
    ChapterBanner('6', 'FieldMapper & Dynamic SQL Builder',
                  'How metadata drives safe parameterised SQL generation'),
    sp(10),
    p('6.1  FetchSqlBuilder', 'h2'),
    hr(P['sky'], 0.8),
    p('FetchSqlBuilder is a pure static function. Same inputs always produce same SQL. '
      'It operates in six stages and always uses the SqlDialect abstraction — '
      'it never branches on provider type directly.', 'body'),
] + code([
    '// FetchSqlBuilder.cs — Dynamic SELECT generation',
    'public static (string Sql, Dictionary<string,object?> Params)',
    '    Build(FetchRequest req, EntityMeta meta, SqlDialect d)',
    '{',
    '    var p = new Dictionary<string, object?>();',
    '    var sb = new StringBuilder();',
    '    var table = $"{d.Schema(meta.SchemaName)}.{d.Quote(meta.EntityName)}";',
    '',
    '    // 1. SELECT clause',
    '    var cols = req.Select?.Any() == true',
    '        ? string.Join(", ", req.Select.Select(c => $"t.{d.Quote(c)}"))',
    '        : "t.*";',
    '    sb.Append($"SELECT {cols} FROM {table} t");',
    '',
    '    // 2. LEFT JOINs for nodes',
    '    int ni = 0;',
    '    foreach (var node in req.Nodes ?? new())',
    '    {',
    '        var nMeta = /* lookup child entity */ null;',
    '        sb.Append($" LEFT JOIN {d.Schema(nMeta.SchemaName)}.{d.Quote(node.NodeEntity)} n{ni}");',
    '        sb.Append($" ON n{ni}.{d.Quote(node.ParentLink)} = t.{d.Quote(meta.PkColumn)}");',
    '        ni++;',
    '    }',
    '',
    '    // 3. WHERE clause — AND-chained filter conditions',
    '    var predicates = new List<string>();',
    '    int pi = 0;',
    '    foreach (var f in req.Filters.Where(f => !string.IsNullOrEmpty(f.Column)))',
    '    {',
    '        var pName = $"p{pi++}";',
    '        predicates.Add(f.Op switch {',
    '            "eq"         => $"t.{d.Quote(f.Column)} = {d.Param(pName)}",',
    '            "neq"        => $"t.{d.Quote(f.Column)} <> {d.Param(pName)}",',
    '            "contains"   => $"LOWER(t.{d.Quote(f.Column)}) LIKE {d.Param(pName)}",',
    '            "startsWith" => $"LOWER(t.{d.Quote(f.Column)}) LIKE {d.Param(pName)}",',
    '            "gt"         => $"t.{d.Quote(f.Column)} > {d.Param(pName)}",',
    '            "gte"        => $"t.{d.Quote(f.Column)} >= {d.Param(pName)}",',
    '            "lt"         => $"t.{d.Quote(f.Column)} < {d.Param(pName)}",',
    '            "lte"        => $"t.{d.Quote(f.Column)} <= {d.Param(pName)}",',
    '            "between"    => $"t.{d.Quote(f.Column)} BETWEEN {d.Param(pName)} AND {d.Param(pName+"b")}",',
    '            "isNull"     => $"t.{d.Quote(f.Column)} IS NULL",',
    '            "isNotNull"  => $"t.{d.Quote(f.Column)} IS NOT NULL",',
    '            _            => throw new SqlBuildException($"Unknown op: {f.Op}")',
    '        });',
    '        SetFilterParam(p, pName, f, d);',
    '    }',
    '',
    '    // 4. Full-text SEARCH — OR across specified columns',
    '    if (req.Search is { Term.Length: > 0 } search)',
    '    {',
    '        var searchCols = search.Columns.Any() ? search.Columns',
    '            : meta.Columns.Where(c => c.DataType == "text").Select(c => c.ColumnName).ToList();',
    '        var likes = searchCols.Select(c => $"LOWER(t.{d.Quote(c)}) LIKE {d.Param("search")}");',
    '        predicates.Add($"({string.Join(" OR ", likes)})");',
    '        p["search"] = $"%{search.Term.ToLower()}%";',
    '    }',
    '',
    '    if (predicates.Any()) sb.Append(" WHERE " + string.Join(" AND ", predicates));',
    '',
    '    // 5. ORDER BY',
    '    if (req.Sort is not null)',
    '        sb.Append($" ORDER BY t.{d.Quote(req.Sort.Column)} {req.Sort.Direction.ToUpper()}");',
    '',
    '    // 6. Pagination',
    '    int skip = (req.Page - 1) * req.PageSize;',
    '    sb.Append(" " + d.PaginationClause(skip, req.PageSize));',
    '',
    '    return (sb.ToString(), p);',
    '}',
], lang='C#') + [
    sp(6),
    p('6.2  ForgeSqlBuilder', 'h2'),
    hr(P['sky'], 0.8),
] + code([
    '// ForgeSqlBuilder.cs — INSERT / UPDATE / soft-DELETE',
    'public static (string Sql, Dictionary<string,object?> P) BuildInsert(',
    '    EntityMeta meta, Dictionary<string,object?> content, SqlDialect d)',
    '{',
    '    // Exclude readonly + audit-only columns from INSERT',
    '    var writeable = meta.Columns',
    '        .Where(c => !c.IsReadOnly && content.ContainsKey(c.FieldName))',
    '        .ToList();',
    '    var cols   = string.Join(", ", writeable.Select(c => d.Quote(c.ColumnName)));',
    '    var pNames = string.Join(", ", writeable.Select(c => d.Param(c.FieldName)));',
    '    var sql    = $"INSERT INTO {d.Schema(meta.SchemaName)}.{d.Quote(meta.EntityName)} ({cols}) VALUES ({pNames}){d.ReturningClause(meta.PkColumn)}";',
    '    var p      = writeable.ToDictionary(c => c.FieldName, c => content[c.FieldName]);',
    '    return (sql, p);',
    '}',
    '',
    'public static (string Sql, Dictionary<string,object?> P) BuildUpdate(',
    '    EntityMeta meta, string id, Dictionary<string,object?> content, SqlDialect d)',
    '{',
    '    var writeable  = meta.Columns',
    '        .Where(c => !c.IsReadOnly && !c.IsPK && content.ContainsKey(c.FieldName))',
    '        .ToList();',
    '    var setClauses = string.Join(", ", writeable.Select(c => $"{d.Quote(c.ColumnName)} = {d.Param(c.FieldName)}"));',
    '    var sql = $"UPDATE {d.Schema(meta.SchemaName)}.{d.Quote(meta.EntityName)} SET {setClauses} WHERE {d.Quote(meta.PkColumn)} = {d.Param("__id")}";',
    '    var p   = writeable.ToDictionary(c => c.FieldName, c => content[c.FieldName]);',
    '    p["__id"] = id;',
    '    return (sql, p);',
    '}',
    '',
    'public static (string Sql, Dictionary<string,object?> P) BuildSoftDelete(',
    '    EntityMeta meta, string id, string deletedBy, SqlDialect d)',
    '{',
    '    var sql = $"UPDATE {d.Schema(meta.SchemaName)}.{d.Quote(meta.EntityName)}',
    '               SET {d.Quote(meta.SoftDeleteColumn)} = {d.Param("__sdv")},',
    '                   deleted_at = {d.Param("__dat")}, deleted_by = {d.Param("__dby")}',
    '               WHERE {d.Quote(meta.PkColumn)} = {d.Param("__id")}";',
    '    return (sql, new() {["__sdv"]=true,["__dat"]=DateTime.UtcNow,["__dby"]=deletedBy,["__id"]=id});',
    '}',
], lang='C#') + [pb()]

# ─────────────────────────────────────────────────────────────────────────────
# CHAPTER 7 — VALIDATION SERVICE
# ─────────────────────────────────────────────────────────────────────────────
story += [
    ChapterBanner('7', 'ValidationService',
                  'Pre-save data validation driven by ColumnRegistry metadata'),
    sp(10),
    p('ValidationService runs before any SQL is built. It reads validation rules from '
      'the ColumnRegistry (required, maxLength, dataType) and can be extended with '
      'business rule plugins per entity.', 'body'),
    sp(4),
    tbl(
        ['Validation Type', 'Source', 'Behaviour on Failure'],
        [
            ['Required field check',   'ColumnRegistry.is_required',  'Adds field to error list — "FieldName is required"'],
            ['Data type check',        'ColumnRegistry.data_type',    'Attempts conversion; on failure adds type mismatch error'],
            ['Max length check',       'ColumnRegistry.max_length',   'Compares string length; adds length error if exceeded'],
            ['Read-only field check',  'ColumnRegistry.is_readonly',  'Removes field from content — does not error'],
            ['Unknown field check',    'ColumnRegistry whitelist',    'Throws SqlBuildException — unknown column is security risk'],
            ['Business rule plugin',   'IValidationRule per entity',  'Plugin returns ValidationResult with custom message'],
        ],
        [48*mm, 45*mm, CW - 93*mm]
    ),
    sp(6),
] + code([
    '// ValidationService.cs',
    'public List<ValidationError> Validate(',
    '    Dictionary<string,object?> content,',
    '    List<ColumnMeta> columns,',
    '    string operation)   // "create" | "update"',
    '{',
    '    var errors = new List<ValidationError>();',
    '',
    '    foreach (var col in columns.Where(c => !c.IsAuditField && !c.IsReadOnly))',
    '    {',
    '        var hasValue = content.TryGetValue(col.FieldName, out var value) && value is not null;',
    '',
    '        // Required check (only on create, or if field explicitly provided on update)',
    '        if (col.IsRequired && !hasValue && operation == "create")',
    '            errors.Add(new(col.FieldName, $"{col.FieldName} is required."));',
    '',
    '        if (!hasValue) continue;',
    '',
    '        // Max length',
    '        if (col.DataType == "text" && col.MaxLength > 0',
    '            && value?.ToString()?.Length > col.MaxLength)',
    '            errors.Add(new(col.FieldName,',
    '                $"{col.FieldName} exceeds maximum length of {col.MaxLength}."));',
    '',
    '        // Type correctness — attempt conversion',
    '        try { _converter.ConvertSingle(value, col.DataType); }',
    '        catch { errors.Add(new(col.FieldName,',
    '            $"{col.FieldName} value \'{value}\' is not valid for type {col.DataType}.")); }',
    '    }',
    '',
    '    // Entity-level business rules',
    '    foreach (var rule in _rules.GetRules(content.GetEntityName()))',
    '        errors.AddRange(rule.Validate(content, operation));',
    '',
    '    return errors;',
    '}',
], lang='C#') + [pb()]

# ─────────────────────────────────────────────────────────────────────────────
# CHAPTER 8 — AUTO NUMBER SERVICE
# ─────────────────────────────────────────────────────────────────────────────
story += [
    ChapterBanner('8', 'AutoNumberService',
                  'Domain-specific key generation with configurable patterns'),
    sp(10),
    p('AutoNumberService generates human-readable sequential identifiers for entities '
      'that require domain keys (invoices, purchase orders, employee codes) rather than '
      'raw UUIDs. It uses a dedicated <b>auto_number_config</b> table with atomic sequence '
      'increments to prevent duplicates under concurrent load.', 'body'),
    sp(4),
    p('auto_number_config Table', 'h3'),
    tbl(
        ['Column', 'Type', 'Example', 'Description'],
        [
            ['config_key',   'VARCHAR(100) PK', 'purchase_order',   'Entity/use-case identifier'],
            ['prefix',       'VARCHAR(20)',      'PO-',              'Static prefix string'],
            ['year_segment', 'BOOLEAN',          'true',             'Append current year (2026)'],
            ['separator',    'VARCHAR(5)',        '-',               'Separator between segments'],
            ['current_seq',  'BIGINT',           '123',             'Current sequence value (atomic++)'],
            ['seq_length',   'INT',              '4',               'Zero-padded length: 0123'],
            ['reset_yearly', 'BOOLEAN',          'true',            'Reset sequence on year change'],
            ['last_reset_yr','INT',              '2026',            'Year of last reset'],
        ],
        [38*mm, 32*mm, 30*mm, CW - 100*mm]
    ),
    sp(4),
    info('&#9654;', 'Generated Key Examples',
         'PO-2026-0123  (purchase_order)  |  INV-2026-00045  (invoice)  |  EMP-1044  (employee)  |  SO202600123  (sales_order)',
         P['teal']),
    sp(6),
] + code([
    '// AutoNumberService.cs',
    'public async Task<string> GenerateAsync(string configKey)',
    '{',
    '    // Atomic fetch-and-increment using SELECT FOR UPDATE (PostgreSQL/MySQL)',
    '    // or UPDATE with OUTPUT (SQL Server) to prevent duplicate generation',
    '    var config = await _db.QuerySingleAsync<AutoNumberConfig>(',
    '        @"SELECT * FROM auto_number_config',
    '          WHERE config_key = @key FOR UPDATE",   // row-level lock',
    '        new { key = configKey });',
    '',
    '    if (config is null) throw new AutoNumberException($"Config \'{configKey}\' not found.");',
    '',
    '    // Reset sequence if new year and reset_yearly=true',
    '    int currentYear = DateTime.UtcNow.Year;',
    '    if (config.ResetYearly && config.LastResetYear < currentYear)',
    '    {',
    '        config.CurrentSeq = 1;',
    '        config.LastResetYear = currentYear;',
    '    }',
    '    else config.CurrentSeq++;',
    '',
    '    await _db.ExecuteAsync(',
    '        "UPDATE auto_number_config SET current_seq=@seq, last_reset_year=@yr WHERE config_key=@key",',
    '        new { seq = config.CurrentSeq, yr = config.LastResetYear, key = configKey });',
    '',
    '    // Build the key: PREFIX + YEAR + SEPARATOR + ZERO-PADDED-SEQ',
    '    var parts = new List<string> { config.Prefix };',
    '    if (config.YearSegment) parts.Add(currentYear.ToString());',
    '    parts.Add(config.CurrentSeq.ToString().PadLeft(config.SeqLength, \'0\'));',
    '    return string.Join(config.Separator, parts);',
    '}',
], lang='C#') + [pb()]

# ─────────────────────────────────────────────────────────────────────────────
# CHAPTER 9 — DATA TYPE CONVERTER
# ─────────────────────────────────────────────────────────────────────────────
story += [
    ChapterBanner('9', 'DataTypeConverter',
                  'Safe runtime conversion of request values to DB-typed .NET objects'),
    sp(10),
    p('All values arriving in <b>Content</b> dictionaries are untyped (object?). '
      'DataTypeConverter maps each value to the correct .NET type based on the '
      'ColumnRegistry data_type before SQL parameters are bound.', 'body'),
    sp(4),
    tbl(
        ['data_type', 'Input Examples', '.NET Target', 'Edge Cases'],
        [
            ['text',    '"hello", 123',          'string',         'null → null; numbers toString'],
            ['int',     '"42", 42, "042"',        'int',            '"" or null → null if not required'],
            ['decimal', '"12.50", 12.5, "12,50"', 'decimal',        'Locale comma handling'],
            ['bool',    '"true", "1", "yes", true','bool',          'Case-insensitive; "0"/"false"/"no" → false'],
            ['date',    '"2026-04-29", "29/04/26"','DateTime (UTC)', 'Multiple ISO + locale formats'],
            ['uuid',    '"550e8400-e29b..."',      'Guid',           'Invalid format throws ConversionException'],
            ['json',    '"{...}"',                 'string (raw)',   'Stored as text; no parsing by engine'],
        ],
        [24*mm, 44*mm, 36*mm, CW - 104*mm]
    ),
    sp(6),
] + code([
    '// DataTypeConverter.cs',
    'public object? ConvertSingle(object? value, string dataType)',
    '{',
    '    if (value is null || value is DBNull) return null;',
    '    var str = value.ToString()?.Trim() ?? "";',
    '    if (str == "") return null;',
    '',
    '    return dataType.ToLower() switch',
    '    {',
    '        "text"    => str,',
    '        "int"     => int.TryParse(str, out var i) ? i',
    '                     : throw new ConversionException($"Cannot convert \'{str}\' to int"),',
    '        "decimal" => decimal.TryParse(str.Replace(",","."),',
    '                     NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d',
    '                     : throw new ConversionException($"Cannot convert \'{str}\' to decimal"),',
    '        "bool"    => str.ToLower() is "true" or "1" or "yes" or "on",',
    '        "date"    => DateTime.TryParse(str, null, DateTimeStyles.AdjustToUniversal,',
    '                     out var dt) ? dt',
    '                     : throw new ConversionException($"Cannot parse date: \'{str}\'"),',
    '        "uuid"    => Guid.TryParse(str, out var g) ? g',
    '                     : throw new ConversionException($"Invalid UUID: \'{str}\'"),',
    '        "json"    => str,   // stored as text; caller validates JSON if needed',
    '        _         => str    // unknown types pass through as string',
    '    };',
    '}',
    '',
    '// Batch convert entire Content dict',
    'public Dictionary<string,object?> Convert(',
    '    Dictionary<string,object?> content, List<ColumnMeta> cols)',
    '{',
    '    var result = new Dictionary<string,object?>();',
    '    foreach (var col in cols)',
    '        if (content.TryGetValue(col.FieldName, out var val))',
    '            result[col.FieldName] = ConvertSingle(val, col.DataType);',
    '    return result;',
    '}',
], lang='C#') + [pb()]

# ─────────────────────────────────────────────────────────────────────────────
# CHAPTER 10 — OPERATION SCOPE
# ─────────────────────────────────────────────────────────────────────────────
story += [
    ChapterBanner('10', 'OperationScope & Transaction Safety',
                  'Atomic parent-child persistence with automatic rollback'),
    sp(10),
    p('OperationScope wraps every forge operation in a database transaction. '
      'It is disposable — if <b>CommitAsync()</b> is never called before disposal, '
      'the transaction is automatically rolled back. This ensures parent + all child '
      'records either all succeed or none persist.', 'body'),
    sp(4),
    info('&#9888;', 'Critical Safety Rule',
         'OperationScope must always be used with "await using" (C# async dispose pattern). '
         'Never call CommitAsync() inside a try block that swallows exceptions — '
         'let the using scope handle rollback on any unhandled exception.',
         P['red']),
    sp(6),
] + code([
    '// OperationScope.cs',
    'public sealed class OperationScope : IAsyncDisposable',
    '{',
    '    private readonly IDbConnection    _conn;',
    '    private readonly IDbTransaction   _tx;',
    '    private bool _committed = false;',
    '',
    '    public static async Task<OperationScope> CreateAsync(string profileId,',
    '        IDbConnectionFactory factory)',
    '    {',
    '        var conn = factory.Create(profileId);',
    '        await conn.OpenAsync();',
    '        var tx = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted);',
    '        return new OperationScope(conn, tx);',
    '    }',
    '',
    '    public async Task ExecuteAsync(string sql, object? param = null)',
    '        => await _conn.ExecuteAsync(sql, param, _tx);',
    '',
    '    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? param = null)',
    '        => await _conn.ExecuteScalarAsync<T>(sql, param, _tx);',
    '',
    '    public async Task CommitAsync()',
    '    {',
    '        await _tx.CommitAsync();',
    '        _committed = true;',
    '    }',
    '',
    '    public async ValueTask DisposeAsync()',
    '    {',
    '        if (!_committed)',
    '        {',
    '            try { await _tx.RollbackAsync(); }',
    '            catch (Exception ex)',
    '                { _logger.LogError(ex, "Rollback failed during OperationScope dispose"); }',
    '        }',
    '        _tx.Dispose();',
    '        await _conn.CloseAsync();',
    '        _conn.Dispose();',
    '    }',
    '}',
    '',
    '// Usage in TransactionService:',
    'await using var scope = await OperationScope.CreateAsync(meta.ProfileId, _factory);',
    'await scope.ExecuteAsync(parentSql, parentParams);',
    'foreach (var child in children)',
    '    await scope.ExecuteAsync(childSql, childParams);',
    'await scope.CommitAsync();   // if this line is never reached → auto rollback',
], lang='C#') + [pb()]

# ─────────────────────────────────────────────────────────────────────────────
# CHAPTER 11 — AUDIT SERVICE
# ─────────────────────────────────────────────────────────────────────────────
story += [
    ChapterBanner('11', 'AuditService',
                  'Before/after diff capture for updates and deletes'),
    sp(10),
    p('AuditService writes to an <b>audit_log</b> table after every successful forge '
      'operation. For creates it stores the new state. For updates it stores an old/new '
      'diff. For deletes it stores the final state before soft-deletion. '
      'Audit writes happen <i>after</i> CommitAsync() so they never block the main transaction.', 'body'),
    sp(4),
    tbl(
        ['Column', 'Type', 'Description'],
        [
            ['log_id',       'UUID PK',       'Audit entry identifier'],
            ['entity_name',  'VARCHAR(200)',   'Table name (e.g. purchase_order)'],
            ['entity_id',    'VARCHAR(100)',   'PK value of the affected row'],
            ['operation',    'VARCHAR(20)',    'create | update | delete'],
            ['changed_by',   'VARCHAR(200)',   'Username from ClaimsPrincipal'],
            ['changed_at',   'TIMESTAMPTZ',   'UTC timestamp'],
            ['old_state',    'JSONB / TEXT',   'Row state before update/delete (null for create)'],
            ['new_state',    'JSONB / TEXT',   'Row state after create/update (null for delete)'],
            ['diff',         'JSONB / TEXT',   'Key-level diff: {col: {old: x, new: y}}'],
            ['ip_address',   'VARCHAR(50)',    'Request IP from HttpContext'],
            ['session_id',   'VARCHAR(100)',   'Optional trace/correlation ID'],
        ],
        [42*mm, 32*mm, CW - 74*mm]
    ),
    sp(6),
] + code([
    '// AuditService.cs',
    'public async Task LogUpdateAsync(',
    '    string entity, string id,',
    '    IDictionary<string,object?> oldState,',
    '    IDictionary<string,object?> newState,',
    '    ClaimsPrincipal user)',
    '{',
    '    // Build key-level diff: only changed fields',
    '    var diff = new Dictionary<string, object>();',
    '    foreach (var key in newState.Keys)',
    '    {',
    '        var oldVal = oldState.TryGetValue(key, out var o) ? o : null;',
    '        var newVal = newState[key];',
    '        if (!Equals(oldVal, newVal))',
    '            diff[key] = new { old = oldVal, @new = newVal };',
    '    }',
    '',
    '    var entry = new AuditLogEntry {',
    '        LogId      = Guid.NewGuid(),',
    '        EntityName = entity,',
    '        EntityId   = id,',
    '        Operation  = "update",',
    '        ChangedBy  = user.Identity?.Name ?? "system",',
    '        ChangedAt  = DateTime.UtcNow,',
    '        OldState   = JsonSerializer.Serialize(oldState),',
    '        NewState   = JsonSerializer.Serialize(newState),',
    '        Diff       = JsonSerializer.Serialize(diff)',
    '    };',
    '',
    '    // Fire-and-forget to separate audit connection — never blocks main transaction',
    '    _ = Task.Run(() => _auditDb.ExecuteAsync(',
    '        @"INSERT INTO audit_log (log_id,entity_name,entity_id,operation,',
    '          changed_by,changed_at,old_state,new_state,diff)',
    '          VALUES (@LogId,@EntityName,@EntityId,@Operation,',
    '                  @ChangedBy,@ChangedAt,@OldState,@NewState,@Diff)", entry));',
    '}',
], lang='C#') + [pb()]

# ─────────────────────────────────────────────────────────────────────────────
# CHAPTER 12 — CONNECTION FACTORY
# ─────────────────────────────────────────────────────────────────────────────
story += [
    ChapterBanner('12', 'ConnectionFactory & Pool Management',
                  'Multi-provider connection lifecycle and health checks'),
    sp(10),
    p('Two factory implementations serve different needs. <b>DatabaseConnectionFactory</b> '
      'provides fast, simple connection creation for CRUD writes. '
      '<b>EnhancedConnectionFactory</b> adds metrics, health checks, and read-replica '
      'routing for high-availability deployments.', 'body'),
    sp(4),
    tbl(
        ['Feature', 'DatabaseConnectionFactory', 'EnhancedConnectionFactory'],
        [
            ['Purpose',             'Standard CRUD writes',                'HA production deployments'],
            ['Connection pool',     'ADO.NET default pool',               'Configurable min/max pool size'],
            ['Health check',        'No',                                  'Periodic ping + status tracking'],
            ['Read replica',        'No',                                  'Routes SELECT to replica by flag'],
            ['Metrics',             'No',                                  'Connection acquire time, pool wait'],
            ['Retry policy',        'None',                                'Polly transient retry (3x, exp backoff)'],
            ['Password handling',   'AES-256 decrypt at startup',         'Same + rotation support'],
            ['Use case',            'Dev / staging / single-DB portals', 'Multi-DB production portals'],
        ],
        [46*mm, 57*mm, CW - 103*mm]
    ),
    sp(6),
] + code([
    '// DatabaseConnectionFactory.cs — Singleton; IDbConnectionFactory',
    'public class DatabaseConnectionFactory : IDbConnectionFactory',
    '{',
    '    // Populated at startup from MetaDB DbProfiles table',
    '    private readonly Dictionary<string, DbProfile> _profiles = new();',
    '',
    '    public IDbConnection Create(string profileId)',
    '    {',
    '        if (!_profiles.TryGetValue(profileId, out var profile))',
    '            throw new ConnectionException($"Profile {profileId} not registered.");',
    '',
    '        var connStr = DecryptConnectionString(profile);',
    '',
    '        return profile.Provider switch',
    '        {',
    '            "postgresql" => new NpgsqlConnection(connStr),',
    '            "mysql"      => new MySqlConnection(connStr),',
    '            "sqlserver"  => new SqlConnection(connStr),',
    '            "oracle"     => new OracleConnection(connStr),',
    '            "sqlite"     => new SqliteConnection(connStr),',
    '            _            => throw new ConnectionException($"Unknown provider: {profile.Provider}")',
    '        };',
    '    }',
    '',
    '    private string DecryptConnectionString(DbProfile p)',
    '    {',
    '        // AES-256-CBC with key from DATA_ENGINE_CRYPTO_KEY env var',
    '        var key = Environment.GetEnvironmentVariable("DATA_ENGINE_CRYPTO_KEY")!;',
    '        return AesEncryption.Decrypt(p.PasswordHash, key, p.Host, p.DatabaseName, p.Username);',
    '    }',
    '}',
], lang='C#') + [pb()]

# ─────────────────────────────────────────────────────────────────────────────
# CHAPTER 13 — SECURITY
# ─────────────────────────────────────────────────────────────────────────────
story += [
    ChapterBanner('13', 'WhitelistValidator & Security Model',
                  'Registry-enforced SQL injection prevention and access control'),
    sp(10),
    p('All security enforcement happens at the <b>WhitelistValidator</b> layer — '
      'before any SQL is constructed. This is the single chokepoint that '
      'prevents unknown table names, unknown column names, and unauthorised role access '
      'from reaching the SQL builder.', 'body'),
    sp(4),
    tbl(
        ['Threat Vector', 'Risk', 'Enforcement Point'],
        [
            ['Unknown entity name in request',   'CRITICAL', 'WhitelistValidator → EntityNotFoundException (404)'],
            ['Unknown column name in Content',   'CRITICAL', 'WhitelistValidator → SqlBuildException (400)'],
            ['Filter on unregistered column',    'HIGH',     'WhitelistValidator on each FilterCondition.Column'],
            ['Filter value injection',           'HIGH',     'All values are Dapper @parameters — never concatenated'],
            ['Write to read-only entity',        'HIGH',     'WhitelistValidator checks EntityMeta.IsReadOnly'],
            ['Role-based table access',          'HIGH',     'AllowedRoles intersection vs ClaimsPrincipal.GetRoles()'],
            ['Plaintext connection strings',     'HIGH',     'AES-256-CBC in DbProfiles; key in env var only'],
            ['Mass data extraction',             'MEDIUM',   'PageSize capped at 1000; no unpaaginated export path'],
            ['Audit bypass',                     'LOW',      'AuditLog has no delete endpoint; append-only by design'],
        ],
        [56*mm, 20*mm, CW - 76*mm]
    ),
    sp(6),
] + code([
    '// WhitelistValidator.cs',
    'public void ValidateForge(ForgeRequest req, EntityMeta meta, ClaimsPrincipal user)',
    '{',
    '    // 1. Entity read-only check',
    '    if (meta.IsReadOnly)',
    '        throw new ReadOnlyViolationException(req.RootEntity);',
    '',
    '    // 2. Role check',
    '    if (meta.AllowedRoles?.Length > 0)',
    '    {',
    '        var userRoles = user.Claims',
    '            .Where(c => c.Type == ClaimTypes.Role)',
    '            .Select(c => c.Value).ToHashSet();',
    '        if (!meta.AllowedRoles.Any(r => userRoles.Contains(r)))',
    '            throw new RoleViolationException(req.RootEntity, meta.AllowedRoles);',
    '    }',
    '',
    '    // 3. Column whitelist — every key in Content must exist in ColumnRegistry',
    '    var knownFields = meta.Columns.Select(c => c.FieldName).ToHashSet(StringComparer.OrdinalIgnoreCase);',
    '    foreach (var key in req.Content.Keys)',
    '        if (!knownFields.Contains(key))',
    '            throw new SqlBuildException(',
    '                $"Column \'{key}\' is not registered for entity \'{req.RootEntity}\'.");',
    '',
    '    // 4. Child node entity checks',
    '    foreach (var node in req.Nodes ?? new())',
    '        if (!_registeredEntities.Contains(node.NodeEntity))',
    '            throw new EntityNotFoundException(node.NodeEntity);',
    '}',
], lang='C#') + [pb()]

# ─────────────────────────────────────────────────────────────────────────────
# CHAPTER 14 — FETCH SERVICE
# ─────────────────────────────────────────────────────────────────────────────
story += [
    ChapterBanner('14', 'FetchService — Read Engine',
                  'Paginated, filtered, searchable data retrieval'),
    sp(10),
    p('FetchService orchestrates all read operations. It is optimised for the most '
      'common ERP query pattern: paginated lists with filters, optional full-text search, '
      'optional child entity joins, and a total count for UI pagination controls.', 'body'),
    sp(4),
] + code([
    '// FetchService.cs',
    'public async Task<FetchResult> ExecuteAsync(FetchRequest req, ClaimsPrincipal user)',
    '{',
    '    var sw   = Stopwatch.StartNew();',
    '    var meta = await _registry.LookupAsync(req.RootEntity);',
    '    _whitelist.ValidateFetch(req, meta, user);',
    '',
    '    var dialect  = SqlDialect.For(meta.Provider);',
    '    using var conn = _factory.Create(meta.ProfileId);',
    '    await conn.OpenAsync();',
    '',
    '    // Build child entity metas for JOINs',
    '    var nodeMetas = new List<EntityMeta>();',
    '    foreach (var node in req.Nodes ?? new())',
    '        nodeMetas.Add(await _registry.LookupAsync(node.NodeEntity));',
    '',
    '    var (sql, p) = FetchSqlBuilder.Build(req, meta, nodeMetas, dialect);',
    '    var data     = (await conn.QueryAsync<dynamic>(sql, p)).ToList();',
    '',
    '    int? total = null;',
    '    if (req.IncludeCount)',
    '    {',
    '        var (countSql, cp) = FetchSqlBuilder.BuildCount(req, meta, dialect);',
    '        total = await conn.ExecuteScalarAsync<int>(countSql, cp);',
    '    }',
    '',
    '    sw.Stop();',
    '    _logger.LogInformation(',
    '        "FetchExecuted {Entity} {Rows} rows page {Page} in {Ms}ms",',
    '        req.RootEntity, data.Count, req.Page, sw.ElapsedMilliseconds);',
    '',
    '    return new FetchResult',
    '    {',
    '        Success    = true,',
    '        Data       = data,',
    '        TotalCount = total,',
    '        Page       = req.Page,',
    '        PageSize   = req.PageSize,',
    '        TotalPages = total.HasValue ? (int)Math.Ceiling(total.Value / (double)req.PageSize) : null',
    '    };',
    '}',
], lang='C#') + [pb()]

# ─────────────────────────────────────────────────────────────────────────────
# CHAPTER 15 — API ENDPOINTS
# ─────────────────────────────────────────────────────────────────────────────
story += [
    ChapterBanner('15', 'API Endpoint Design',
                  'HTTP contracts for all engine capabilities'),
    sp(10),
    tbl(
        ['Method', 'Endpoint', 'Service', 'Description'],
        [
            ['POST',   '/api/forge',                          'TransactionService', 'Create, update, or soft-delete any registered entity'],
            ['POST',   '/api/fetch',                          'FetchService',       'Paginated read with filters, search, sort, joins'],
            ['GET',    '/api/fetch/{entity}/{id}',            'FetchService',       'Get single record by primary key'],
            ['GET',    '/api/registry/profiles',              'RegistryController', 'List all DB profiles'],
            ['POST',   '/api/registry/profiles',              'RegistryController', 'Register new DB profile'],
            ['PUT',    '/api/registry/profiles/{id}',         'RegistryController', 'Update DB profile'],
            ['DELETE', '/api/registry/profiles/{id}',         'RegistryController', 'Remove DB profile'],
            ['POST',   '/api/registry/profiles/{id}/test',    'RegistryController', 'Test DB connection, return latency'],
            ['POST',   '/api/registry/profiles/{id}/discover','RegistryController', 'Introspect DB, return unregistered tables'],
            ['POST',   '/api/registry/profiles/{id}/invalidate','RegistryController','Flush registry cache for profile'],
            ['GET',    '/api/registry/entities',              'RegistryController', 'List entities for a profile'],
            ['POST',   '/api/registry/entities',              'RegistryController', 'Register new entity'],
            ['PUT',    '/api/registry/entities/{id}',         'RegistryController', 'Update entity metadata'],
            ['DELETE', '/api/registry/entities/{id}',         'RegistryController', 'Remove entity from registry'],
            ['GET',    '/api/registry/entities/{id}/columns', 'RegistryController', 'List columns for entity'],
            ['POST',   '/api/registry/entities/{id}/columns', 'RegistryController', 'Add column to entity'],
            ['DELETE', '/api/registry/columns/{id}',          'RegistryController', 'Remove column from registry'],
            ['GET',    '/api/autonumber/configs',             'AutoNumberController','List auto-number configurations'],
            ['GET',    '/api/audit/{entity}/{id}',            'AuditController',    'Get audit history for a record'],
        ],
        [18*mm, 70*mm, 40*mm, CW - 128*mm]
    ),
    sp(6),
    p('Unified Error Response Envelope', 'h2'),
    hr(P['sky'], 0.8),
] + code([
    '// All error responses use this envelope',
    '{',
    '  "success": false,',
    '  "error":   "Entity \'unknown_table\' is not registered in the engine registry.",',
    '  "errorCode": "ENTITY_NOT_FOUND",',
    '  "field":   null,           // populated for validation errors',
    '  "traceId": "00-4bf5122f-1a2b3c4d-01"',
    '}',
    '',
    '// HTTP status code mapping:',
    '// EntityNotFoundException        → 404',
    '// ValidationException            → 400  (errors[] array in response)',
    '// RoleViolationException         → 403',
    '// ReadOnlyViolationException     → 403',
    '// SqlBuildException              → 400',
    '// ConnectionException            → 503',
    '// AutoNumberException            → 500',
    '// Unhandled exceptions           → 500  (details hidden in production)',
], lang='JSON') + [pb()]

# ─────────────────────────────────────────────────────────────────────────────
# CHAPTER 16 — REACT FRONTEND
# ─────────────────────────────────────────────────────────────────────────────
story += [
    ChapterBanner('16', 'React Frontend Suite',
                  'Registry Manager · Query Builder · Field Mapper'),
    sp(10),
    p('The frontend is a React 18 + TypeScript admin suite built with Ant Design 5.x '
      'and TanStack Query v5. All three pages are driven by the registry API — '
      'they never hardcode table or column names.', 'body'),
    sp(4),
    tbl(
        ['Page', 'Route', 'Primary Purpose', 'Calls'],
        [
            ['Registry Manager', '/admin/registry',      'Admin: manage DB profiles, entities, columns, auto-numbers, audit log', '/api/registry/*'],
            ['Query Builder',    '/admin/query',          'Developer: visual /fetch query composer with live results',             '/api/fetch'],
            ['Field Mapper',     '/admin/forge',          'User: visual /forge payload builder for create/update operations',      '/api/forge'],
        ],
        [36*mm, 32*mm, 72*mm, CW - 140*mm]
    ),
    sp(6),
    p('Frontend Component Tree', 'h2'),
    hr(P['sky'], 0.8),
] + code([
    'src/',
    '├── types/',
    '│   ├── FetchRequest.ts         Zod schema + inferred type',
    '│   ├── ForgeRequest.ts         Zod schema + inferred type',
    '│   └── Registry.ts             DbProfile, EntityMeta, ColumnMeta, AutoNumberConfig',
    '├── services/',
    '│   ├── fetchApi.ts             POST /api/fetch → FetchResult',
    '│   ├── forgeApi.ts             POST /api/forge → ForgeResult',
    '│   ├── registryApi.ts          CRUD + testConnection + discoverTables',
    '│   └── auditApi.ts             GET /api/audit/{entity}/{id}',
    '├── pages/',
    '│   ├── RegistryManager.tsx',
    '│   ├── QueryBuilder.tsx',
    '│   └── FieldMapper.tsx',
    '└── components/',
    '    ├── registry/',
    '    │   ├── ProfileCard.tsx     Connection status + test button',
    '    │   ├── ProfileForm.tsx     Add/edit DB profile modal',
    '    │   ├── EntityCard.tsx      Expandable entity with column list',
    '    │   ├── EntityForm.tsx      Add/edit entity modal',
    '    │   ├── ColumnPanel.tsx     Column add/edit/remove drawer',
    '    │   └── AutoNumberConfig.tsx Auto-number setup panel',
    '    ├── querybuilder/',
    '    │   ├── FilterRow.tsx       Column + operator + value with type-aware input',
    '    │   ├── ColumnSelector.tsx  Checkbox list from ColumnRegistry',
    '    │   ├── SortConfig.tsx      Column select + ASC/DESC toggle',
    '    │   ├── NodeConfig.tsx      Child JOIN entity selector',
    '    │   ├── PaginationConfig.tsx Page + pageSize + includeCount',
    '    │   └── ResultsTable.tsx    Dynamic column headers + pagination',
    '    └── fieldmapper/',
    '        ├── ColumnRow.tsx       Field name + type badge + value input',
    '        └── ChildSection.tsx    Child entity FK config + column rows',
], lang='STRUCTURE') + [
    sp(4),
    info('&#9654;', 'TanStack Query Pattern',
         'All API calls use useMutation (forge) and useQuery (fetch/registry). '
         'Registry data is cached with staleTime=5min matching backend CacheTtlMinutes. '
         'Cache invalidation after registry mutations uses queryClient.invalidateQueries.',
         P['teal']),
    pb(),
]

# ─────────────────────────────────────────────────────────────────────────────
# CHAPTER 17 — ERROR HANDLING
# ─────────────────────────────────────────────────────────────────────────────
story += [
    ChapterBanner('17', 'Error Handling Strategy',
                  'Exception hierarchy, middleware, and result types'),
    sp(10),
] + code([
    '// Exception hierarchy',
    'DataEngineException  (base : Exception)',
    '  ├── EntityNotFoundException      → HTTP 404',
    '  ├── ReadOnlyViolationException   → HTTP 403',
    '  ├── RoleViolationException       → HTTP 403',
    '  ├── SqlBuildException            → HTTP 400',
    '  ├── ValidationException          → HTTP 400  (carries List<ValidationError>)',
    '  ├── AutoNumberException          → HTTP 500',
    '  └── ConnectionException          → HTTP 503',
    '',
    '// DataEngineExceptionMiddleware.cs',
    'public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)',
    '{',
    '    try { await next(ctx); }',
    '    catch (DataEngineException ex)',
    '    {',
    '        ctx.Response.StatusCode = ex switch {',
    '            EntityNotFoundException  => 404,',
    '            RoleViolationException   => 403,',
    '            ReadOnlyViolationException => 403,',
    '            ValidationException      => 400,',
    '            SqlBuildException        => 400,',
    '            ConnectionException      => 503,',
    '            _                        => 500',
    '        };',
    '        ctx.Response.ContentType = "application/json";',
    '        var envelope = new {',
    '            success   = false,',
    '            error     = ex.Message,',
    '            errorCode = ex.GetType().Name.Replace("Exception","").ToUpperInvariant().Replace("VIOLATION","_VIOLATION"),',
    '            errors    = ex is ValidationException ve ? ve.Errors : null,',
    '            traceId   = Activity.Current?.TraceId.ToString()',
    '        };',
    '        await ctx.Response.WriteAsJsonAsync(envelope);',
    '        _logger.LogWarning(ex, "DataEngine handled exception {Code}", envelope.errorCode);',
    '    }',
    '}',
], lang='C#') + [pb()]

# ─────────────────────────────────────────────────────────────────────────────
# CHAPTER 18 — TESTING
# ─────────────────────────────────────────────────────────────────────────────
story += [
    ChapterBanner('18', 'Testing Strategy',
                  'Unit, integration, and contract test coverage'),
    sp(10),
    tbl(
        ['Layer', 'Framework', 'Scope', 'Target Count'],
        [
            ['Unit',        'xUnit + Moq',         'SqlBuilders (all operators), DataTypeConverter, AutoNumberService, WhitelistValidator, ValidationService', '~140 tests'],
            ['Integration', 'xUnit + SQLite in-memory', 'FetchService, TransactionService, AuditService against real schema', '~50 tests'],
            ['Provider',    'xUnit + Testcontainers', 'Dialect SQL output verified against real PostgreSQL/MySQL Docker containers', '~30 tests'],
            ['Contract',    'Pact.NET',             'Consumer-driven contracts for /forge and /fetch request schemas', '~20 tests'],
            ['E2E',         'Playwright',           'Registry Manager + Query Builder + Field Mapper against staging', '~15 tests'],
        ],
        [24*mm, 38*mm, 80*mm, CW - 142*mm]
    ),
    sp(6),
    p('Key Unit Test Cases', 'h2'),
    hr(P['sky'], 0.8),
]
for b in [
    'FetchSqlBuilder: "between" operator produces BETWEEN @p0 AND @p0b with both parameter values',
    'FetchSqlBuilder: "contains" wraps value in %...% and uses LOWER() for case-insensitive match',
    'FetchSqlBuilder: pagination clause uses LIMIT/OFFSET for PostgreSQL/MySQL vs OFFSET ROWS FETCH NEXT for SQL Server/Oracle',
    'ForgeSqlBuilder: INSERT excludes is_readonly columns; UPDATE excludes PK and readonly columns',
    'ForgeSqlBuilder: soft-delete produces UPDATE SET is_deleted=true — never DELETE FROM',
    'DataTypeConverter: "true", "1", "yes", "on" all convert to boolean true',
    'DataTypeConverter: empty string converts to null for nullable fields',
    'AutoNumberService: concurrent GenerateAsync calls never produce duplicates (using FOR UPDATE lock)',
    'ValidationService: required field missing on create produces error; missing on update is allowed',
    'WhitelistValidator: unknown column name throws SqlBuildException before any SQL is built',
    'OperationScope: uncommitted scope disposes with rollback — no partial inserts persist',
]:
    story.append(bul(b))
story.append(pb())

# ─────────────────────────────────────────────────────────────────────────────
# CHAPTER 19 — ROADMAP
# ─────────────────────────────────────────────────────────────────────────────
story += [
    ChapterBanner('19', 'Enhancement Roadmap',
                  'Priority improvements identified from reference architecture analysis'),
    sp(10),
    p('The following improvements are prioritised based on the reference architecture '
      'analysis, targeting the known risks in dynamic SQL engines and unlocking '
      'enterprise-scale capabilities.', 'body'),
    sp(4),
    p('Phase 1 — Security & Stability (Weeks 1–2)', 'h2'),
    hr(P['red'], 0.8),
]
for b in [
    '<b>Replace any string SQL concatenation with ForgeSqlBuilder parameterised output</b> — audit all provider implementations for raw string interpolation in SQL',
    '<b>Add ColumnRegistry whitelist check for every filter column</b> in /fetch requests — WhitelistValidator.ValidateFetch must cover FilterCondition.Column',
    '<b>AES-256-CBC for all DbProfile password fields</b> — add migration script for existing plaintext entries',
    '<b>Add pageSize cap of 1000</b> to FetchService — no path should allow unpaaginated full-table reads',
]:
    story.append(bul(b))

story += [sp(4), p('Phase 2 — Performance (Weeks 3–4)', 'h2'), hr(P['amber'], 0.8)]
for b in [
    '<b>Cache FieldMapper metadata in IMemoryCache</b> with 5-minute TTL — eliminate repeated MetaDB reads per request',
    '<b>Add bulk insert path</b> to ForgeSqlBuilder — for Nodes[] with 50+ child records, generate VALUES (...),(...),...  instead of N individual INSERT statements',
    '<b>Instrument every Dapper call with Stopwatch</b> — log Warning if execution &gt;500ms, Error if &gt;2000ms',
    '<b>Add Serilog structured events</b> on every forge/fetch with entity, provider, rowCount, executionMs',
]:
    story.append(bul(b))

story += [sp(4), p('Phase 3 — Capability (Weeks 5–8)', 'h2'), hr(P['green'], 0.8)]
for b in [
    '<b>Soft-delete strategy</b> — configure SoftDeleteColumn/SoftDeleteValue per entity in EntityRegistry; all "delete" operations set this flag rather than DELETE FROM',
    '<b>Multi-provider Testcontainers integration tests</b> — verify dialect SQL against real PostgreSQL and MySQL Docker containers in CI',
    '<b>Business rule plugin system</b> — IValidationRule interface loadable per entity from DI container',
    '<b>Bulk import endpoint</b> — POST /api/forge/bulk accepts array of ForgeRequests in single transaction',
    '<b>Auto-discover enhancement</b> — introspect INFORMATION_SCHEMA to auto-populate ColumnRegistry on import, inferring data types from DB column types',
]:
    story.append(bul(b))

story += [
    sp(4),
    p('Architecture Decision Summary', 'h2'),
    hr(P['sky'], 0.8),
    tbl(
        ['Decision', 'Recommendation', 'Rationale'],
        [
            ['ORM vs Dapper',       'Keep Dapper',                   'Dynamic SQL generated at runtime cannot be modelled with EF Core\'s static DbSet<T>'],
            ['Registry storage',    'MetaDB (dedicated)',            'Runtime mutability, UI governance, FK integrity, no redeploy for new entity'],
            ['Soft delete',         'Always soft',                   'Manufacturing ERP data is legally auditable; physical deletes break audit chains'],
            ['Auto number lock',    'SELECT FOR UPDATE row lock',    'Prevents duplicate keys under concurrent load at cost of brief row contention'],
            ['Audit write timing',  'Post-commit fire-and-forget',  'Audit failures must never roll back business data'],
            ['Cache invalidation',  'Explicit + TTL hybrid',        'Admin endpoint flushes immediately; TTL acts as safety net'],
        ],
        [40*mm, 42*mm, CW - 82*mm]
    ),
    sp(6),
    hr(P['border']),
    p('Dynamic Data Engine  ·  Architecture & Developer Guide  ·  v2.0  ·  Internal Engineering  ·  Confidential', 'caption'),
]

# ─────────────────────────────────────────────────────────────────────────────
# BUILD
# ─────────────────────────────────────────────────────────────────────────────
doc = SimpleDocTemplate(
    '/mnt/user-data/outputs/DataEngine_Architecture_Guide_v2.pdf',
    pagesize=A4,
    leftMargin=MARGIN, rightMargin=MARGIN,
    topMargin=18*mm, bottomMargin=15*mm,
    title='Dynamic Data Engine — Architecture & Developer Guide v2.0',
    author='Engineering',
    subject='Metadata-Driven CRUD Engine Reference',
)

# Ensure outputs directory exists and prefer workspace-local path for content-only builds
out_dir = Path('outputs')
out_dir.mkdir(parents=True, exist_ok=True)
out_path = out_dir / ('DataEngine_Architecture_Guide_v2_content.pdf' if CONTENT_ONLY else 'DataEngine_Architecture_Guide_v2.pdf')
doc = SimpleDocTemplate(
    str(out_path),
    pagesize=A4,
    leftMargin=MARGIN, rightMargin=MARGIN,
    topMargin=18*mm, bottomMargin=15*mm,
    title='Dynamic Data Engine — Architecture & Developer Guide v2.0',
    author='Engineering',
    subject='Metadata-Driven CRUD Engine Reference',
)

def first_page(c, doc):
    cover_page(c, doc)

doc.build(story, onFirstPage=first_page, onLaterPages=draw_page)
print(f'✓ PDF generated: {out_path}')
