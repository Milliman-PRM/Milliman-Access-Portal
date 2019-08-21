import '../../../images/icons/add.svg';
import '../../../images/icons/user.svg';

import * as React from 'react';
import { connect } from 'react-redux';
import ReduxToastr from 'react-redux-toastr';

import {
  isPublicationActive, PublicationStatus,
} from '../../view-models/content-publishing';
import {
  Client, ClientWithStats, ContentAssociatedFileType, RootContentItem,
  RootContentItemWithPublication,
} from '../models';
import { ActionIcon } from '../shared-components/action-icon';
import { CardPanel } from '../shared-components/card-panel/card-panel';
import {
    PanelSectionToolbar, PanelSectionToolbarButtons,
} from '../shared-components/card-panel/panel-sections';
import { Card } from '../shared-components/card/card';
import CardButton from '../shared-components/card/card-button';
import {
   CardSectionButtons, CardSectionMain, CardSectionStats, CardText,
} from '../shared-components/card/card-sections';
import { CardStat } from '../shared-components/card/card-stat';
import {
  ContentPanel, ContentPanelSectionContent,
} from '../shared-components/content-panel/content-panel';
import { Filter } from '../shared-components/filter';
import { FileUploadInput } from '../shared-components/form/file-upload-input';
import {
  FormInputContainer, FormSection, FormSectionContainer, FormSectionDivider,
} from '../shared-components/form/form-elements';
import { Input, TextAreaInput } from '../shared-components/form/input';
import { Select } from '../shared-components/form/select';
import { NavBar } from '../shared-components/navbar';
import * as PublishingActionCreators from './redux/action-creators';
import {
  activeSelectedClient, activeSelectedItem, availableAssociatedContentTypes,
  availableContentTypes, clientEntities, itemEntities, selectedItem,
} from './redux/selectors';
import {
    PublishingFormData, PublishingState, PublishingStateCardAttributes, PublishingStateFilters,
    PublishingStatePending, PublishingStateSelected,
} from './redux/store';

type ClientEntity = (ClientWithStats & { indent: 1 | 2 }) | 'divider';
interface RootContentItemEntity extends RootContentItemWithPublication {
  contentTypeName: string;
}

interface ContentPublishingProps {
  clients: ClientEntity[];
  items: RootContentItemEntity[];
  contentTypes: Array<{ selectionValue: string | number, selectionLabel: string }>;
  associatedContentTypes: Array<{ selectionValue: string | number, selectionLabel: string }>;
  formData: PublishingFormData;
  selected: PublishingStateSelected;
  cardAttributes: PublishingStateCardAttributes;
  pending: PublishingStatePending;
  filters: PublishingStateFilters;

  selectedItem: RootContentItem;
  activeSelectedClient: Client;
  activeSelectedItem: RootContentItem;
}

class ContentPublishing extends React.Component<ContentPublishingProps & typeof PublishingActionCreators> {
  private readonly currentView: string = document
    .getElementsByTagName('body')[0].getAttribute('data-nav-location');

  public componentDidMount() {
    this.props.fetchGlobalData({});
    this.props.fetchClients({});
    this.props.scheduleStatusRefresh({ delay: 0 });
    this.props.scheduleSessionCheck({ delay: 0 });
    // setUnloadAlert(() => this.props.pending.item);
  }

  public render() {
    return (
      <>
        <ReduxToastr
          timeOut={5000}
          newestOnTop={false}
          position="bottom-center"
          transitionIn="fadeIn"
          transitionOut="fadeOut"
        />
        <NavBar currentView={this.currentView} />
        {this.renderClientPanel()}
        {this.props.selected.client && this.renderItemPanel()}
        {this.props.selected.item && this.props.formData.formData.clientId && this.renderContentItemForm()}
      </>
    );
  }

  private renderClientPanel() {
    const { clients, selected, filters, pending, cardAttributes } = this.props;
    return (
      <CardPanel
        entities={clients}
        loading={pending.data.clients}
        renderEntity={(entity, key) => {
          if (entity === 'divider') {
            return <div className="hr" key={key} />;
          }
          const card = cardAttributes.client[entity.id];
          return (
            <Card
              key={key}
              selected={selected.client === entity.id}
              disabled={card.disabled}
              onSelect={() => {
                if (selected.client !== entity.id) {
                  this.props.fetchItems({ clientId: entity.id });
                }
                this.props.selectClient({ id: entity.id });
              }}
              indentation={entity.indent}
            >
              <CardSectionMain>
                <CardText text={entity.name} subtext={entity.code} />
                <CardSectionStats>
                  <CardStat
                    name={'Reports'}
                    value={entity.contentItemCount}
                    icon={'reports'}
                  />
                  <CardStat
                    name={'Users'}
                    value={entity.userCount}
                    icon={'user'}
                  />
                </CardSectionStats>
              </CardSectionMain>
            </Card>
          );
        }}
      >
        <h3 className="admin-panel-header">Clients</h3>
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter clients...'}
            setFilterText={(text) => this.props.setFilterTextClient({ text })}
            filterText={filters.client.text}
          />
          <PanelSectionToolbarButtons>
            <div id="icons" />
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
      </CardPanel>
    );
  }

  private renderItemPanel() {
    const { activeSelectedClient: activeClient, items, selected, filters, pending } = this.props;
    const createNewContentItemIcon = (
      <ActionIcon
        label="New Content Item"
        icon="add"
        action={() => { alert('Create New Content Item'); }}
      />
    );
    return activeClient && (
      <CardPanel
        entities={items}
        loading={pending.data.items}
        renderEntity={(entity, key) => {
          const cardButtons = entity.status.requestStatus === PublicationStatus.Processed ?
            (
              <>
                <CardButton
                  color={'green'}
                  tooltip={'Approve'}
                  onClick={() => alert('Go Live Preview')}
                  icon={'checkmark'}
                />
              </>
            ) : entity.status.requestStatus === PublicationStatus.Queued
              || entity.status.requestStatus === PublicationStatus.Validating ? (
                <>
                  <CardButton
                    color={'red'}
                    tooltip={'Cancel'}
                    onClick={() => alert('cancel')}
                    icon={'cancel'}
                  />
                </>
              ) : (
                <>
                  <CardButton
                    color={'red'}
                    tooltip={'Delete'}
                    onClick={() => alert('delete')}
                    icon={'delete'}
                  />
                  <CardButton
                    color={'green'}
                    tooltip={'Edit'}
                    onClick={() => alert('upload')}
                    icon={'edit'}
                  />
                </>
              );
          return (
            <Card
              key={key}
              selected={selected.item === entity.id}
              onSelect={() => {
                if (selected.item !== entity.id) {
                  this.props.fetchContentItemDetail({ rootContentItemId: entity.id });
                }
                this.props.selectItem({ id: entity.id });
              }}
              suspended={entity.isSuspended}
              status={entity.status}
            >
              <CardSectionMain>
                <CardText
                  text={entity.name}
                  textSuffix={entity.isSuspended ? '[Suspended]' : ''}
                  subtext={entity.contentTypeName}
                />
                <CardSectionStats>
                  <CardStat
                    name={'Selection groups'}
                    value={entity.selectionGroupCount}
                    icon={'group'}
                  />
                  <CardStat
                    name={'Assigned users'}
                    value={entity.assignedUserCount}
                    icon={'user'}
                  />
                </CardSectionStats>
                <CardSectionButtons>
                  {cardButtons}
                </CardSectionButtons>
              </CardSectionMain>
            </Card>
            );
        }}
        renderNewEntityButton={() => (
          <div className="card-container action-card-container" onClick={() => alert('Content Item Created')}>
            <div className="admin-panel-content">
              <div className="card-body-container card-100 action-card">
                <h2 className="card-body-primary-text">
                  <svg className="action-card-icon">
                    <use href="#add" />
                  </svg>
                  <span>CREATE CONTENT ITEM</span>
                </h2>
              </div>
            </div>
          </div>
        )}
      >
        <h3 className="admin-panel-header">Content Items</h3>
        <PanelSectionToolbar>
          <Filter
            placeholderText={'Filter content items...'}
            setFilterText={(text) => this.props.setFilterTextItem({ text })}
            filterText={filters.item.text}
          />
          <PanelSectionToolbarButtons>
            {createNewContentItemIcon}
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
      </CardPanel>
    );
  }

  private renderContentItemForm() {
    const { contentTypes, associatedContentTypes, formData: dataForForm, pending } = this.props;
    const { formErrors, formData, uploads } = dataForForm;
    const contentItemFormButtons = (
      <ActionIcon
        label="Close Form"
        icon="cancel"
        action={() => { alert('Cancel Form'); }}
      />
    );
    const associatedContent = <div>Add Associated Content</div>;
    return (
      <ContentPanel loading={pending.data.contentItemDetail}>
        <h3 className="admin-panel-header">Content Item</h3>
        <PanelSectionToolbar>
          <PanelSectionToolbarButtons>
            {contentItemFormButtons}
          </PanelSectionToolbarButtons>
        </PanelSectionToolbar>
        <ContentPanelSectionContent>
          <form autoComplete="off">
            <FormSectionContainer>
              <FormSection title="Content Item Information">
                <FormSectionDivider>
                  <FormInputContainer flexPhone={12}>
                    <Input
                      autoFocus={true}
                      error={formErrors.contentName}
                      label="Content Name"
                      name="contentName"
                      onBlur={() => false}
                      onChange={({ currentTarget: target }: React.FormEvent<HTMLInputElement>) => {
                        this.props.setPublishingFormTextInputValue({
                          inputName: 'contentName',
                          value: target.value,
                        });
                      }}
                      type="text"
                      value={formData.contentName}
                    />
                  </FormInputContainer>
                  <FormInputContainer flexPhone={12} flexTablet={4}>
                    <Select
                      error={dataForForm.formErrors.contentTypeId}
                      label="Content Type"
                      name="contentType"
                      onChange={({ currentTarget: target }: React.FormEvent<HTMLSelectElement>) => {
                        this.props.setPublishingFormTextInputValue({
                          inputName: 'contentTypeId',
                          value: target.value,
                        });
                      }}
                      placeholderText="Choose Content Type"
                      value={dataForForm.formData.contentTypeId}
                      values={contentTypes}
                    />
                  </FormInputContainer>
                  <FormInputContainer flexPhone={12} flexTablet={8}>
                    <FileUploadInput
                      fileExtensions={['qvw', 'pbix', 'pdf']}
                      label="Master Content"
                      name="masterContent"
                      placeholderText="Upload Master Content"
                      beginUpload={(uploadId, fileName) =>
                        this.props.beginFileUpload({ uploadId, fileName })}
                      cancelFileUpload={() => false}
                      finalizeUpload={() => alert('upload succeeded')}
                      setUploadError={(uploadId, errorMsg) =>
                        this.props.setUploadError({ uploadId, errorMsg })}
                      updateChecksumProgress={(uploadId, progress) =>
                        this.props.updateChecksumProgress({ uploadId, progress })}
                      updateUploadProgress={(uploadId, progress) =>
                        this.props.updateUploadProgress({ uploadId, progress })}
                      upload={uploads[formData.relatedFiles.MasterContent.uniqueUploadId]}
                      uploadId={formData.relatedFiles.MasterContent.uniqueUploadId}
                      value={formData.relatedFiles.MasterContent.fileOriginalName}
                    />
                  </FormInputContainer>
                </FormSectionDivider>
              </FormSection>
            </FormSectionContainer>
            <FormSectionContainer>
              <FormSection title="Content Related Files">
                <FormSectionDivider>
                  <FormInputContainer flexPhone={4}>
                    <FileUploadInput
                      fileExtensions={['image/*']}
                      label="Thumbnail"
                      name="thumbnail"
                      placeholderText="Upload Thumbnail"
                      beginUpload={(uploadId, fileName) =>
                        this.props.beginFileUpload({ uploadId, fileName })}
                      cancelFileUpload={() => false}
                      finalizeUpload={() => alert('upload succeeded')}
                      setUploadError={(uploadId, errorMsg) =>
                        this.props.setUploadError({ uploadId, errorMsg })}
                      updateChecksumProgress={(uploadId, progress) =>
                        this.props.updateChecksumProgress({ uploadId, progress })}
                      updateUploadProgress={(uploadId, progress) =>
                        this.props.updateUploadProgress({ uploadId, progress })}
                      upload={uploads[formData.relatedFiles.Thumbnail.uniqueUploadId]}
                      uploadId={formData.relatedFiles.Thumbnail.uniqueUploadId}
                      value={formData.relatedFiles.Thumbnail.fileOriginalName}
                    />
                  </FormInputContainer>
                  <FormInputContainer flexPhone={8}>
                    <FileUploadInput
                      fileExtensions={['pdf']}
                      label="User Guide"
                      name="userGuide"
                      placeholderText="Upload User Guide"
                      beginUpload={(uploadId, fileName) =>
                        this.props.beginFileUpload({ uploadId, fileName })}
                      cancelFileUpload={() => false}
                      finalizeUpload={() => alert('upload succeeded')}
                      setUploadError={(uploadId, errorMsg) =>
                        this.props.setUploadError({ uploadId, errorMsg })}
                      updateChecksumProgress={(uploadId, progress) =>
                        this.props.updateChecksumProgress({ uploadId, progress })}
                      updateUploadProgress={(uploadId, progress) =>
                        this.props.updateUploadProgress({ uploadId, progress })}
                      upload={uploads[formData.relatedFiles.UserGuide.uniqueUploadId]}
                      uploadId={formData.relatedFiles.UserGuide.uniqueUploadId}
                      value={formData.relatedFiles.UserGuide.fileOriginalName}
                    />
                    <FileUploadInput
                      fileExtensions={['pdf']}
                      label="Release Notes"
                      name="releaseNotes"
                      placeholderText="Upload Release Notes"
                      beginUpload={(uploadId, fileName) =>
                        this.props.beginFileUpload({ uploadId, fileName })}
                      cancelFileUpload={() => false}
                      finalizeUpload={() => alert('upload succeeded')}
                      setUploadError={(uploadId, errorMsg) =>
                        this.props.setUploadError({ uploadId, errorMsg })}
                      updateChecksumProgress={(uploadId, progress) =>
                        this.props.updateChecksumProgress({ uploadId, progress })}
                      updateUploadProgress={(uploadId, progress) =>
                        this.props.updateUploadProgress({ uploadId, progress })}
                      upload={uploads[formData.relatedFiles.ReleaseNotes.uniqueUploadId]}
                      uploadId={formData.relatedFiles.ReleaseNotes.uniqueUploadId}
                      value={formData.relatedFiles.ReleaseNotes.fileOriginalName}
                    />
                  </FormInputContainer>
                </FormSectionDivider>
              </FormSection>
            </FormSectionContainer>
            <FormSectionContainer>
              <FormSection title="Content Description">
                <FormSectionDivider>
                  <FormInputContainer flexPhone={12}>
                    <TextAreaInput
                      error={dataForForm.formErrors.contentDescription}
                      label="Content Description"
                      name="contentDescription"
                      onBlur={() => false}
                      onChange={({ currentTarget: target }: React.FormEvent<HTMLTextAreaElement>) => {
                        this.props.setPublishingFormTextInputValue({
                          inputName: 'contentDescription',
                          value: target.value,
                        });
                      }}
                      placeholderText="Content Description..."
                      value={dataForForm.formData.contentDescription}
                    />
                  </FormInputContainer>
                  <FormInputContainer flexPhone={12}>
                    <TextAreaInput
                      error={dataForForm.formErrors.contentDisclaimer}
                      label="Custom Disclaimer Text"
                      name="contentDisclaimer"
                      onBlur={() => false}
                      onChange={({ currentTarget: target }: React.FormEvent<HTMLTextAreaElement>) => {
                        this.props.setPublishingFormTextInputValue({
                          inputName: 'contentDisclaimer',
                          value: target.value,
                        });
                      }}
                      placeholderText="Custom Disclaimer Text..."
                      value={dataForForm.formData.contentDisclaimer}
                    />
                  </FormInputContainer>
                </FormSectionDivider>
              </FormSection>
            </FormSectionContainer>
            <FormSectionContainer>
              <FormSection title="Associated Content">
                <FormSectionDivider>
                  {associatedContent}
                </FormSectionDivider>
              </FormSection>
            </FormSectionContainer>
            <FormSectionContainer>
              <FormSection title="Internale Notes (Not Shown To End Users)">
                <FormSectionDivider>
                  <FormInputContainer flexPhone={12}>
                    <TextAreaInput
                      error={dataForForm.formErrors.contentNotes}
                      label="Notes"
                      name="contentDisclaimer"
                      onBlur={() => false}
                      onChange={({ currentTarget: target }: React.FormEvent<HTMLTextAreaElement>) => {
                        this.props.setPublishingFormTextInputValue({
                          inputName: 'contentNotes',
                          value: target.value,
                        });
                      }}
                      placeholderText="Notes..."
                      value={dataForForm.formData.contentNotes}
                    />
                  </FormInputContainer>
                </FormSectionDivider>
              </FormSection>
            </FormSectionContainer>
            <button
              onClick={(event: any) => {
                event.preventDefault();
                this.props.resetContentItemForm({});
              }}
            >
              Reset Form
            </button>
          </form>
        </ContentPanelSectionContent>
      </ContentPanel>
    );
  }

}

function mapStateToProps(state: PublishingState): ContentPublishingProps {
  const { formData, selected, cardAttributes, pending, filters } = state;
  return {
    clients: clientEntities(state),
    items: itemEntities(state),
    contentTypes: availableContentTypes(state),
    associatedContentTypes: availableAssociatedContentTypes(state),
    formData,
    selected,
    cardAttributes,
    pending,
    filters,
    selectedItem: selectedItem(state),
    activeSelectedClient: activeSelectedClient(state),
    activeSelectedItem: activeSelectedItem(state),
  };
}

export const ConnectedContentPublishing = connect(
  mapStateToProps,
  PublishingActionCreators,
)(ContentPublishing);
